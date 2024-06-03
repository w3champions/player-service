namespace player_service_net_test4.Models;

public class FriendMessage
{
    private FriendMessage(string value) { Value = value; }

    public string Value { get; private set; }

    public static FriendMessage LoadAllResponse { get { return new FriendMessage("LoadAllResponse"); } }
    public static FriendMessage LoadFriendListResponse { get { return new FriendMessage("LoadFriendListResponse"); } }
    public static FriendMessage FriendRequestSuccess { get { return new FriendMessage("FriendRequestSuccess"); } }
    public static FriendMessage FriendResponseMessage { get { return new FriendMessage("FriendResponseMessage"); } }
    public static FriendMessage DeleteOutgoingFriendRequestResponse { get { return new FriendMessage("DeleteOutgoingFriendRequestResponse"); } }
    
    public static FriendMessage AcceptIncomingFriendRequestResponse { get { return new FriendMessage("AcceptIncomingFriendRequestResponse"); } }
    public static FriendMessage DenyIncomingFriendRequestResponse { get { return new FriendMessage("DenyIncomingFriendRequestResponse"); } }
    public static FriendMessage BlockIncomingFriendRequestResponse { get { return new FriendMessage("BlockIncomingFriendRequestResponse"); } }
    public static FriendMessage UnblockFriendRequestsFromPlayerResponse { get { return new FriendMessage("UnblockFriendRequestsFromPlayerResponse"); } }
    public static FriendMessage RemoveFriendResponse { get { return new FriendMessage("RemoveFriendResponse"); } }

    public override string ToString()
    {
        return Value;
    }
}
