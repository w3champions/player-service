using Microsoft.AspNetCore.SignalR;

namespace player_service_net_test4.Hubs;

public class PlayerHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
