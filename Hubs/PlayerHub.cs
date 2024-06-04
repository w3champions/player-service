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
        var user = await _authenticationService.GetUser(accessToken);
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

    internal async Task LoginAsAuthenticated(User user)
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

    public async Task MakeFriendRequest(string receiver)
    {
        var currentUser = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (currentUser == null) return;
        try {
            var user = await _websiteBackendService.GetUser(receiver) ?? throw new ValidationException($"Player {receiver} not found.");

            if (currentUser.Equals(receiver, StringComparison.CurrentCultureIgnoreCase)) {
                throw new ValidationException("Cannot request yourself as a friend.");
            }
            var sentRequests = await _friendRequestCache.LoadSentFriendRequests(currentUser);
            if (sentRequests.Count > 10) {
                throw new ValidationException("You have too many pending friend requests.");
            }
            var request = new FriendRequest(currentUser, receiver);
            var receiverFriendlist = await _friendRepository.LoadFriendList(receiver);
            await CanMakeFriendRequest(receiverFriendlist, request);
            await _friendRepository.CreateFriendRequest(request);
            _friendRequestCache.Insert(request);
            sentRequests.Add(request);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), null, sentRequests, null, $"Friend request sent to {receiver}!");

            var requestsReceivedByOtherPlayer = await _friendRequestCache.LoadReceivedFriendRequests(receiver);
            PushDataToOtherPlayer(receiver, null, null, requestsReceivedByOtherPlayer);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task DeleteOutgoingFriendRequest(string receiver)
    {
        var currentUser = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (currentUser == null) return;
        try {
            var request = await _friendRequestCache.LoadFriendRequest(currentUser, receiver) ?? throw new ValidationException("Could not find a friend request to delete.");
            await _friendRepository.DeleteFriendRequest(request);
            _friendRequestCache.Delete(request);

            List<FriendRequest> sentRequests = await _friendRequestCache.LoadSentFriendRequests(currentUser);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), null, sentRequests, null, $"Friend request to {receiver} deleted!");

            var requestsReceivedByOtherPlayer = await _friendRequestCache.LoadReceivedFriendRequests(receiver);
            PushDataToOtherPlayer(receiver, null, null, requestsReceivedByOtherPlayer);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task AcceptIncomingFriendRequest(string sender)
    {
        var currentUser = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (currentUser == null) return;
        try {
            var currentUserFriendlist = await _friendRepository.LoadFriendList(currentUser);
            var senderFriendlist = await _friendRepository.LoadFriendList(sender);

            var request = await _friendRequestCache.LoadFriendRequest(sender, currentUser) ?? throw new ValidationException("Could not find a friend request to accept.");
            await _friendRepository.DeleteFriendRequest(request);
            _friendRequestCache.Delete(request);
            var reciprocalRequest = await _friendRequestCache.LoadFriendRequest(currentUser, sender);
            if (reciprocalRequest != null) {
                await _friendRepository.DeleteFriendRequest(reciprocalRequest);
                _friendRequestCache.Delete(reciprocalRequest);
            }

            if (!currentUserFriendlist.Friends.Contains(sender)) {
                currentUserFriendlist.Friends.Add(sender);
            }
            await _friendRepository.UpsertFriendList(currentUserFriendlist);

            if (!senderFriendlist.Friends.Contains(currentUser)) {
                senderFriendlist.Friends.Add(currentUser);
            }
            await _friendRepository.UpsertFriendList(senderFriendlist);

            List<FriendRequest> sentRequests = await _friendRequestCache.LoadSentFriendRequests(currentUser);
            List<FriendRequest> receivedRequests = await _friendRequestCache.LoadReceivedFriendRequests(currentUser);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), currentUserFriendlist, sentRequests, receivedRequests, $"Friend request from {sender} accepted!");

            var requestsSentByOtherPlayer = await _friendRequestCache.LoadSentFriendRequests(sender);
            PushDataToOtherPlayer(sender, senderFriendlist, requestsSentByOtherPlayer);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task DenyIncomingFriendRequest(string sender)
    {
        var currentUser = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (currentUser == null) return;
        try {
            var request = await _friendRequestCache.LoadFriendRequest(sender, currentUser) ?? throw new ValidationException("Could not find a friend request to deny.");
            await _friendRepository.DeleteFriendRequest(request);
            _friendRequestCache.Delete(request);

            List<FriendRequest> receivedRequests = await _friendRequestCache.LoadReceivedFriendRequests(currentUser);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), null, null, receivedRequests, $"Friend request from {sender} denied!");

            var sentRequests = await _friendRequestCache.LoadSentFriendRequests(sender);
            PushDataToOtherPlayer(sender, null, sentRequests);
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task BlockIncomingFriendRequest(string sender)
    {
        var currentUser = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (currentUser == null) return;
        try {
            var currentUserFriendlist = await _friendRepository.LoadFriendList(currentUser);
            CanBlock(currentUserFriendlist, sender);

            var request = await _friendRequestCache.LoadFriendRequest(sender, currentUser) ?? throw new ValidationException("Could not find a friend request to block.");
            await _friendRepository.DeleteFriendRequest(request);
            _friendRequestCache.Delete(request);

            currentUserFriendlist.BlockedBattleTags.Add(sender);
            await _friendRepository.UpsertFriendList(currentUserFriendlist);

            List<FriendRequest> receivedRequests = await _friendRequestCache.LoadReceivedFriendRequests(currentUser);
            await Clients.Caller.SendAsync(FriendMessageResponse.FriendResponseData.ToString(), currentUserFriendlist, null, receivedRequests, $"Friend requests from {sender} blocked!");

            var sentRequests = await _friendRequestCache.LoadSentFriendRequests(sender);
            PushDataToOtherPlayer(sender, null, sentRequests);
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

            PushDataToOtherPlayer(friend, otherUserFriendlist);
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

    private async void PushDataToOtherPlayer(
        string battleTag,
        FriendList? friendList = null,
        List<FriendRequest>? sentRequests = null,
        List<FriendRequest>? receivedRequests = null,
        string? message = null
    ) {
        var otherPlayer = _connections.GetUsers().FirstOrDefault(x => x.BattleTag == battleTag);
        if (otherPlayer?.ConnectionId == null) return;
        await Clients.Client(otherPlayer.ConnectionId).SendAsync(FriendMessageResponse.FriendResponseData.ToString(), friendList, sentRequests, receivedRequests, message);
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
