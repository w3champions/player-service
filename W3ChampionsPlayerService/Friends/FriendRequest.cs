using MongoDB.Bson;

namespace W3ChampionsPlayerService.Friends;

public class FriendRequest(string sender, string receiver)
{
    public ObjectId Id { get; set; }
    public string Sender { get; set; } = sender;
    public string Receiver { get; set; } = receiver;
}
