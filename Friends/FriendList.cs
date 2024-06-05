namespace W3ChampionsPlayerService.Friends;

public class FriendList(string battleTag) : IIdentifiable
{
    public string Id { get; set; } = battleTag;
    public List<string> Friends { get; set; } = new List<string> { };
    public List<string> BlockedBattleTags { get; set; } = new List<string> { };
    public bool BlockAllRequests { get; set; } = false;
}
