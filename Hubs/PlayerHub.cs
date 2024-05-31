using Microsoft.AspNetCore.SignalR;
using player_service_net_test4.Authentication;
using player_service_net_test4.Models;

namespace player_service_net_test4.Hubs;

public class PlayerHub : Hub
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ConnectionMapping _connections;
    private readonly IHttpContextAccessor _contextAccessor;

    public PlayerHub(
        IAuthenticationService authenticationService,
        ConnectionMapping connections,
        IHttpContextAccessor contextAccessor)
    {
        _authenticationService = authenticationService;
        _connections = connections;
        _contextAccessor = contextAccessor;
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
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
