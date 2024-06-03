namespace player_service_net_test4.Models;

public class FriendMessageResponse
{
    private FriendMessageResponse(string value) { Value = value; }

    public string Value { get; private set; }

    public static FriendMessageResponse FriendResponseMessage { get { return new FriendMessageResponse("FriendResponseMessage"); } }
    public static FriendMessageResponse FriendResponseData { get { return new FriendMessageResponse("FriendResponseData"); } }
    public static FriendMessageResponse PushFriends { get { return new FriendMessageResponse("PushFriends"); } }

    public override string ToString()
    {
        return Value;
    }
}
