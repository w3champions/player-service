namespace player_service_net_test4.Models;

public class FriendResponseType
{
    private FriendResponseType(string value) { Value = value; }

    public string Value { get; private set; }

    public static FriendResponseType FriendResponseMessage { get { return new FriendResponseType("FriendResponseMessage"); } }
    public static FriendResponseType FriendResponseData { get { return new FriendResponseType("FriendResponseData"); } }
    public static FriendResponseType PushFriends { get { return new FriendResponseType("PushFriends"); } }

    public override string ToString()
    {
        return Value;
    }
}
