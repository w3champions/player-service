namespace player_service_net_test4.Models;

public class WebSocketUser(string battleTag)
{
    public string BattleTag { get; set; } = battleTag;
    public string? ConnectionId {get; set; }
}
