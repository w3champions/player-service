using Microsoft.AspNetCore.SignalR;
using player_service_net_test4.Authentication;
using player_service_net_test4.Friends;
using player_service_net_test4.Models;
using player_service_net_test4.Services;
using System.ComponentModel.DataAnnotations;

namespace player_service_net_test4.Hubs;

public class PlayerHub(
    IAuthenticationService authenticationService,
    ConnectionMapping connections,
    IHttpContextAccessor contextAccessor,
    FriendRepository friendRepository,
    IWebsiteBackendRepository websiteBackendRepository) : Hub
{
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly ConnectionMapping _connections = connections;
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    private readonly FriendRepository _friendRepository = friendRepository;
    private readonly IWebsiteBackendRepository _websiteBackendRepository = websiteBackendRepository;
    // requestsBySender = Map<battletag, friendrequest[]> and requestsByReceiver = Map<battletag, friendrequest[]>
    // or friendRequests = friendrequest[] and I filter either here or on the frontend.

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
        await LoginAsAuthenticated(user);
        await base.OnConnectedAsync();
    }

    internal async Task LoginAsAuthenticated(User user)
    {
        _connections.Add(Context.ConnectionId, user);
        await LoadAll(user.BattleTag);
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task LoadAll(string battleTag)
    {
        FriendList friendList = await _friendRepository.LoadFriendList(battleTag);
        List<FriendRequest> sentRequests = await _friendRepository.LoadFriendRequestsSentByPlayer(battleTag);
        List<FriendRequest> receivedRequests = await _friendRepository.LoadFriendRequestsSentToPlayer(battleTag);
        await Clients.Caller.SendAsync(FriendMessage.LoadAllResponse.ToString(), friendList, sentRequests, receivedRequests);
    }

    public async Task LoadFriendList(string battleTag)
    {
        FriendList friendList = await _friendRepository.LoadFriendList(battleTag);
        await Clients.Caller.SendAsync(FriendMessage.LoadFriendListResponse.ToString(), friendList);
    }

    public async Task MakeFriendRequest(string receiver)
    {
        var sender = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (sender == null) return;
        try {
            var user = await _websiteBackendRepository.GetUser(receiver) ?? throw new ValidationException($"Player {receiver} not found.");

            if (sender.Equals(receiver, StringComparison.CurrentCultureIgnoreCase)) {
                throw new ValidationException("Cannot request yourself as a friend.");
            }
            var requestsMadeByPlayer = await _friendRepository.LoadFriendRequestsSentByPlayer(sender);
            if (requestsMadeByPlayer.Count > 10) {
                throw new ValidationException("You have too many pending friend requests.");
            }
            var request = new FriendRequest(sender, receiver);
            var receiverFriendlist = await _friendRepository.LoadFriendList(receiver);
            await CanMakeFriendRequest(receiverFriendlist, request);
            await _friendRepository.CreateFriendRequest(request);
            List<FriendRequest> sentRequests = await _friendRepository.LoadFriendRequestsSentByPlayer(sender);
            await Clients.Caller.SendAsync(FriendMessage.FriendRequestSuccess.ToString(), sentRequests, $"Friend request sent to {receiver}!");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessage.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task DeleteOutgoingFriendRequest(string receiver)
    {
        var sender = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (sender == null) return;
        try {
            var request = await _friendRepository.LoadFriendRequest(sender, receiver) ?? throw new ValidationException("Could not find a friend request to delete.");
            await _friendRepository.DeleteFriendRequest(request);

            List<FriendRequest> sentRequests = await _friendRepository.LoadFriendRequestsSentByPlayer(sender);
            await Clients.Caller.SendAsync(FriendMessage.DeleteOutgoingFriendRequestResponse.ToString(), sentRequests, $"Friend request to {receiver} deleted!");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessage.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task AcceptIncomingFriendRequest(string sender)
    {
        var receiver = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (receiver == null) return;
        try {
            var currentUserFriendlist = await _friendRepository.LoadFriendList(receiver);
            var otherUserFriendlist = await _friendRepository.LoadFriendList(sender);

            var request = await _friendRepository.LoadFriendRequest(sender, receiver) ?? throw new ValidationException("Could not find a friend request to accept.");
            await _friendRepository.DeleteFriendRequest(request);
            var reciprocalRequest = await _friendRepository.LoadFriendRequest(receiver, sender) ?? throw new ValidationException("Could not find reciprocal friend request.");
            await _friendRepository.DeleteFriendRequest(reciprocalRequest);

            if (!currentUserFriendlist.Friends.Contains(sender)) {
                currentUserFriendlist.Friends.Add(sender);
            }
            await _friendRepository.UpsertFriendList(currentUserFriendlist);

            if (!otherUserFriendlist.Friends.Contains(receiver)) {
                otherUserFriendlist.Friends.Add(receiver);
            }
            await _friendRepository.UpsertFriendList(otherUserFriendlist);

            FriendList friendList = await _friendRepository.LoadFriendList(receiver);
            List<FriendRequest> sentRequests = await _friendRepository.LoadFriendRequestsSentByPlayer(receiver);
            List<FriendRequest> receivedRequests = await _friendRepository.LoadFriendRequestsSentToPlayer(receiver);
            await Clients.Caller.SendAsync(FriendMessage.AcceptIncomingFriendRequestResponse.ToString(), friendList, sentRequests, receivedRequests, $"Friend request from {sender} accepted!");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessage.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task DenyIncomingFriendRequest(string sender)
    {
        var receiver = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (receiver == null) return;
        try {
            var request = await _friendRepository.LoadFriendRequest(sender, receiver) ?? throw new ValidationException("Could not find a friend request to deny.");
            await _friendRepository.DeleteFriendRequest(request);

            List<FriendRequest> receivedRequests = await _friendRepository.LoadFriendRequestsSentToPlayer(receiver);
            await Clients.Caller.SendAsync(FriendMessage.DenyIncomingFriendRequestResponse.ToString(), receivedRequests, $"Friend request from {sender} denied!");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessage.FriendResponseMessage.ToString(), ex.Message);
        }
    }

    public async Task BlockIncomingFriendRequest(string sender)
    {
        var receiver = _connections.GetUser(Context.ConnectionId)?.BattleTag;
        if (receiver == null) return;
        try {
            var currentUserFriendlist = await _friendRepository.LoadFriendList(receiver);
            CanBlock(currentUserFriendlist, sender);

            var request = await _friendRepository.LoadFriendRequest(sender, receiver) ?? throw new ValidationException("Could not find a friend request to block.");
            await _friendRepository.DeleteFriendRequest(request);

            currentUserFriendlist.BlockedBattleTags.Add(sender);
            await _friendRepository.UpsertFriendList(currentUserFriendlist);

            FriendList friendList = await _friendRepository.LoadFriendList(receiver);
            List<FriendRequest> receivedRequests = await _friendRepository.LoadFriendRequestsSentToPlayer(receiver);
            await Clients.Caller.SendAsync(FriendMessage.BlockIncomingFriendRequestResponse.ToString(), friendList, receivedRequests, $"Friend requests from {sender} blocked!");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessage.FriendResponseMessage.ToString(), ex.Message);
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

            await Clients.Caller.SendAsync(FriendMessage.UnblockFriendRequestsFromPlayerResponse.ToString(), friendList, $"Friend requests from {battleTag} unblocked!");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessage.FriendResponseMessage.ToString(), ex.Message);
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

            FriendList friendList = await _friendRepository.LoadFriendList(currentUser);
            await Clients.Caller.SendAsync(FriendMessage.RemoveFriendResponse.ToString(), friendList, $"Removed {friend} from friends.");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync(FriendMessage.FriendResponseMessage.ToString(), ex.Message);
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

    private async Task CanMakeFriendRequest(FriendList friendList, FriendRequest req) {
        if (friendList.BlockAllRequests || friendList.BlockedBattleTags.Contains(req.Sender)) {
            throw new ValidationException("This player is not accepting friend requests.");
        }
        if (friendList.Friends.Contains(req.Sender)) {
            throw new ValidationException("You are already friends with this player.");
        }
        var requestAlreadyExists = await _friendRepository.FriendRequestExists(req);
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
