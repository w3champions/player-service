using Microsoft.AspNetCore.SignalR;
using player_service_net_test4.Authentication;
using player_service_net_test4.Friends;
using player_service_net_test4.Models;
using player_service_net_test4.Services;
using System.ComponentModel.DataAnnotations;

namespace player_service_net_test4.Hubs;

public class PlayerHub(
    AuthenticationService authenticationService,
    ConnectionMapping connections,
    IHttpContextAccessor contextAccessor,
    FriendRepository friendRepository,
    WebsiteBackendService websiteBackendService,
    FriendRequestCache friendRequestCache) : Hub
{
    private readonly AuthenticationService _authenticationService = authenticationService;
    private readonly ConnectionMapping _connections = connections;
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    private readonly FriendRepository _friendRepository = friendRepository;
    private readonly WebsiteBackendService _websiteBackendService = websiteBackendService;
    private readonly FriendRequestCache _friendRequestCache = friendRequestCache;

    public override async Task OnConnectedAsync()
    {
        var accessToken = _contextAccessor?.HttpContext?.Request.Query["access_token"];
        WebSocketUser? user = await _authenticationService.GetUser(accessToken);
        if (user == null)
        {
            await Clients.Caller.SendAsync("AuthorizationFailed");
            Context.Abort();
            return;
        }
        user.ConnectionId = Context.ConnectionId;
        await LoginAsAuthenticated(user);
        await base.OnConnectedAsync();
    }

    internal async Task LoginAsAuthenticated(WebSocketUser user)
    {
        _connections.Add(Context.ConnectionId, user);
        await LoadAll(user.BattleTag);
    }

    public async Task LoadAll(string battleTag)
    {
        FriendList friendList = await _friendRepository.LoadFriendList(battleTag);
        List<FriendRequest> sentRequests = await _friendRequestCache.LoadSentFriendRequests(battleTag);
        List<FriendRequest> receivedRequests = await _friendRequestCache.LoadReceivedFriendRequests(battleTag);
        await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), friendList, sentRequests, receivedRequests);
    }

    public async Task LoadFriends()
    {
        var currentUser = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (currentUser == null) return;
        FriendList friendList = await _friendRepository.LoadFriendList(currentUser);
        var users = await _websiteBackendService.GetUsers(friendList.Friends);
        await Clients.Caller.SendAsync(FriendMessageResponse.PushFriends.ToString(), users);
    }

    public async Task<List<User>> GetFriends(string battleTag)
    {
        FriendList friendList = await _friendRepository.LoadFriendList(battleTag);
        return await _websiteBackendService.GetUsers(friendList.Friends) ?? [];
    }

    public async Task MakeFriendRequest(FriendRequest req)
    {
        try {
            var user = await _websiteBackendService.GetUser(req.Receiver) ?? throw new ValidationException($"Player {req.Receiver} not found.");

            if (req.Sender.Equals(req.Receiver, StringComparison.CurrentCultureIgnoreCase)) {
                throw new ValidationException("Cannot request yourself as a friend.");
            }
            var sentRequests = await _friendRequestCache.LoadSentFriendRequests(req.Sender);
            if (sentRequests.Count > 10) {
                throw new ValidationException("You have too many pending friend requests.");
            }
            var receiverFriendlist = await _friendRepository.LoadFriendList(req.Receiver);
            await CanMakeFriendRequest(receiverFriendlist, req);
            await _friendRepository.CreateFriendRequest(req);
            _friendRequestCache.Insert(req);
            sentRequests.Add(req);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), null, sentRequests, null, $"Friend request sent to {req.Receiver}!");

            var requestsReceivedByOtherPlayer = await _friendRequestCache.LoadReceivedFriendRequests(req.Receiver);
            await PushFriendResponseDataToPlayer(req.Receiver, null, null, requestsReceivedByOtherPlayer);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task DeleteOutgoingFriendRequest(FriendRequest req)
    {
        try {
            var request = await _friendRequestCache.LoadFriendRequest(req) ?? throw new ValidationException("Could not find a friend request to delete.");
            await _friendRepository.DeleteFriendRequest(request);
            _friendRequestCache.Delete(request);

            List<FriendRequest> sentRequests = await _friendRequestCache.LoadSentFriendRequests(req.Sender);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), null, sentRequests, null, $"Friend request to {req.Receiver} deleted!");

            var requestsReceivedByOtherPlayer = await _friendRequestCache.LoadReceivedFriendRequests(req.Receiver);
            await PushFriendResponseDataToPlayer(req.Receiver, null, null, requestsReceivedByOtherPlayer);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task AcceptIncomingFriendRequest(FriendRequest req)
    {
        try {
            var currentUserFriendlist = await _friendRepository.LoadFriendList(req.Receiver);
            var senderFriendlist = await _friendRepository.LoadFriendList(req.Sender);

            var request = await _friendRequestCache.LoadFriendRequest(req) ?? throw new ValidationException("Could not find a friend request to accept.");
            await _friendRepository.DeleteFriendRequest(request);
            _friendRequestCache.Delete(request);
            var reciprocalRequest = await _friendRequestCache.LoadFriendRequest(new FriendRequest(req.Receiver, req.Sender));
            if (reciprocalRequest != null) {
                await _friendRepository.DeleteFriendRequest(reciprocalRequest);
                _friendRequestCache.Delete(reciprocalRequest);
            }

            if (!currentUserFriendlist.Friends.Contains(req.Sender)) {
                currentUserFriendlist.Friends.Add(req.Sender);
            }
            await _friendRepository.UpsertFriendList(currentUserFriendlist);

            if (!senderFriendlist.Friends.Contains(req.Receiver)) {
                senderFriendlist.Friends.Add(req.Receiver);
            }
            await _friendRepository.UpsertFriendList(senderFriendlist);

            List<FriendRequest> sentRequests = await _friendRequestCache.LoadSentFriendRequests(req.Receiver);
            List<FriendRequest> receivedRequests = await _friendRequestCache.LoadReceivedFriendRequests(req.Receiver);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), currentUserFriendlist, sentRequests, receivedRequests, $"Friend request from {req.Sender} accepted!");

            List<User> receiverFriends = await GetFriends(req.Receiver);
            await Clients.Caller.SendAsync(FriendMessageResponse.PushFriends.ToString(), receiverFriends);

            await PushFriendsToPlayer(req.Sender);
            var requestsSentByOtherPlayer = await _friendRequestCache.LoadSentFriendRequests(req.Sender);
            await PushFriendResponseDataToPlayer(req.Sender, senderFriendlist, requestsSentByOtherPlayer);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task DenyIncomingFriendRequest(FriendRequest req)
    {
        try {
            var request = await _friendRequestCache.LoadFriendRequest(req) ?? throw new ValidationException("Could not find a friend request to deny.");
            await _friendRepository.DeleteFriendRequest(request);
            _friendRequestCache.Delete(request);

            List<FriendRequest> receivedRequests = await _friendRequestCache.LoadReceivedFriendRequests(req.Receiver);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), null, null, receivedRequests, $"Friend request from {req.Sender} denied!");

            var sentRequests = await _friendRequestCache.LoadSentFriendRequests(req.Sender);
            await PushFriendResponseDataToPlayer(req.Sender, null, sentRequests);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task BlockIncomingFriendRequest(FriendRequest req)
    {
        try {
            var currentUserFriendlist = await _friendRepository.LoadFriendList(req.Receiver);
            CanBlock(currentUserFriendlist, req.Sender);

            var request = await _friendRequestCache.LoadFriendRequest(req) ?? throw new ValidationException("Could not find a friend request to block.");
            await _friendRepository.DeleteFriendRequest(request);
            _friendRequestCache.Delete(request);

            currentUserFriendlist.BlockedBattleTags.Add(req.Sender);
            await _friendRepository.UpsertFriendList(currentUserFriendlist);

            List<FriendRequest> receivedRequests = await _friendRequestCache.LoadReceivedFriendRequests(req.Receiver);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), currentUserFriendlist, null, receivedRequests, $"Friend requests from {req.Sender} blocked!");

            var sentRequests = await _friendRequestCache.LoadSentFriendRequests(req.Sender);
            await PushFriendResponseDataToPlayer(req.Sender, null, sentRequests);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task UnblockFriendRequestsFromPlayer(string battleTag)
    {
        var currentUser = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (currentUser == null) return;
        try {
            var friendList = await _friendRepository.LoadFriendList(currentUser);

            var itemToRemove = friendList.BlockedBattleTags.SingleOrDefault(bTag => bTag == battleTag) ?? throw new ValidationException("Could not find a player to unblock.");
            friendList.BlockedBattleTags.Remove(itemToRemove);
            await _friendRepository.UpsertFriendList(friendList);

            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), friendList, null, null, $"Friend requests from {battleTag} unblocked!");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task RemoveFriend(string friend)
    {
        var currentUser = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (currentUser == null) return;
        try {
            var currentUserFriendlist = await _friendRepository.LoadFriendList(currentUser);
            var friendRelation1 = currentUserFriendlist.Friends.SingleOrDefault(bTag => bTag == friend);
            if (friendRelation1 != null) {
                currentUserFriendlist.Friends.Remove(friendRelation1);
            }
            await _friendRepository.UpsertFriendList(currentUserFriendlist);

            var otherUserFriendlist = await _friendRepository.LoadFriendList(friend);
            var friendRelation2 = otherUserFriendlist.Friends.SingleOrDefault(bTag => bTag == currentUser);
            if (friendRelation2 != null) {
                otherUserFriendlist.Friends.Remove(friendRelation2);
            }
            await _friendRepository.UpsertFriendList(otherUserFriendlist);

            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), currentUserFriendlist, null, null, $"Removed {friend} from friends.");

            List<User> currentUserFriends = await GetFriends(currentUser);
            await Clients.Caller.SendAsync(FriendMessageResponse.PushFriends.ToString(), currentUserFriends);

            await PushFriendsToPlayer(friend);

            await PushFriendResponseDataToPlayer(friend, otherUserFriendlist);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = _connections.GetUser(Context.ConnectionId);
        if (user != null)
        {
            _connections.Remove(Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task PushFriendResponseDataToPlayer(
        string battleTag,
        FriendList? friendList = null,
        List<FriendRequest>? sentRequests = null,
        List<FriendRequest>? receivedRequests = null,
        string? message = null
    ) {
        var player = _connections.GetUsers().FirstOrDefault(x => x.BattleTag == battleTag);
        if (player?.ConnectionId == null) return;
        await Clients.Client(player.ConnectionId).SendAsync(FriendMessageResponse.FriendResponseData.ToString(), friendList, sentRequests, receivedRequests, message);
    }

    private async Task PushFriendsToPlayer(string battleTag) {
        var player = _connections.GetUsers().FirstOrDefault(x => x.BattleTag == battleTag);
        if (player?.ConnectionId == null) return;
        List<User> friends = await GetFriends(battleTag);
        await Clients.Client(player.ConnectionId).SendAsync(FriendMessageResponse.PushFriends.ToString(), friends);
    }

    private async Task CanMakeFriendRequest(FriendList friendList, FriendRequest req) {
        if (friendList.BlockAllRequests || friendList.BlockedBattleTags.Contains(req.Sender)) {
            throw new ValidationException("This player is not accepting friend requests.");
        }
        if (friendList.Friends.Contains(req.Sender)) {
            throw new ValidationException("You are already friends with this player.");
        }
        var requestAlreadyExists = await _friendRequestCache.FriendRequestExists(req);
        if (requestAlreadyExists) {
            throw new ValidationException("You have already requested to be friends with this player.");
        }
    }

    private static void CanBlock(FriendList friendList, string battleTag) {
        if (friendList.BlockedBattleTags.Contains(battleTag)) {
            throw new ValidationException("You have already blocked this player.");
        }
        if (friendList.Friends.Contains(battleTag)) {
            throw new ValidationException("You cannot block a player you are friends with.");
        }
    }
}
