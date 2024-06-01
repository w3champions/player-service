using Microsoft.AspNetCore.SignalR;
using player_service_net_test4.Authentication;
using player_service_net_test4.Friends;
using player_service_net_test4.Models;
using player_service_net_test4.Services;
using System.ComponentModel.DataAnnotations;

namespace player_service_net_test4.Hubs;

public class PlayerHub : Hub
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ConnectionMapping _connections;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly FriendRepository _friendRepository;
    private readonly IWebsiteBackendRepository _websiteBackendRepository;

    public PlayerHub(
        IAuthenticationService authenticationService,
        ConnectionMapping connections,
        IHttpContextAccessor contextAccessor,
        FriendRepository friendRepository,
        IWebsiteBackendRepository websiteBackendRepository)
    {
        _authenticationService = authenticationService;
        _connections = connections;
        _contextAccessor = contextAccessor;
        _friendRepository = friendRepository;
        _websiteBackendRepository = websiteBackendRepository;
    }
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
        await LoadFriendList(user.BattleTag);
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task LoadFriendList(string battleTag)
    {
        FriendList friendList = await _friendRepository.LoadFriendList(battleTag);
        await Clients.Caller.SendAsync("SetFriendList", friendList);
    }

    public async Task MakeFriendRequest(string sender, string receiver)
    {
        try {
            var userDetails = await _websiteBackendRepository.GetPlayerProfile(receiver);
            if (userDetails == null) {
                await Clients.Caller.SendAsync("MakeFriendRequestError", $"Player {receiver} not found.");
                return;
            }
            if (sender.Equals(receiver, StringComparison.CurrentCultureIgnoreCase)) {
                await Clients.Caller.SendAsync("MakeFriendRequestError", $"Cannot request yourself as a friend.");
                return;
            }
            var allRequestsMadeByPlayer = await _friendRepository.LoadAllFriendRequestsSentByPlayer(sender);
            if (allRequestsMadeByPlayer.Count > 10) {
                await Clients.Caller.SendAsync("MakeFriendRequestError", $"You have too many pending friend requests.");
                return;
            }
            var request = new FriendRequest(sender, receiver);
            var receiverFriendlist = await _friendRepository.LoadFriendList(receiver);
            await CanMakeFriendRequest(receiverFriendlist, request);
            await _friendRepository.CreateFriendRequest(request);
            await Clients.Caller.SendAsync("MakeFriendRequestSuccess", $"Friend request sent to {receiver}!");
        } catch (Exception ex) {
            await Clients.Caller.SendAsync("MakeFriendRequestError", ex.Message);
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

    private async Task CanMakeFriendRequest(FriendList friendlist, FriendRequest req) {
        if (friendlist.BlockAllRequests || friendlist.BlockedBattleTags.Contains(req.Sender)) {
            throw new ValidationException("This player is not accepting friend requests.");
        }
        if (friendlist.Friends.Contains(req.Sender)) {
            throw new ValidationException("You are already friends with this player.");
        }

        var requestAlreadyExists = await _friendRepository.FriendRequestExists(req);
        if (requestAlreadyExists) {
            throw new ValidationException("You have already requested to be friends with this player.");
        }
    }
}
