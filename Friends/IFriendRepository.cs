namespace player_service_net_test4.Friends;

public interface IFriendRepository
{
    Task<FriendList> LoadFriendList(string battleTag);
    Task UpsertFriendList(FriendList friendList);
    Task<FriendRequest> CreateFriendRequest(FriendRequest req);
    Task<FriendRequest> LoadFriendRequest(FriendRequest req);
    Task<bool> FriendRequestExists(FriendRequest req);
    Task DeleteFriendRequest(FriendRequest req);
    Task<List<FriendRequest>> LoadSentFriendRequests(string sender);
    Task<List<FriendRequest>> LoadReceivedFriendRequests(string receiver);
}
