namespace player_service_net_test4.Friends;

public interface IFriendRepository
{
    Task<FriendList> LoadFriendList(string battleTag);
    Task UpsertFriendList(FriendList friendList);
    Task<FriendRequest> CreateFriendRequest(FriendRequest request);
    Task<FriendRequest> LoadFriendRequest(string sender, string receiver);
    Task<bool> FriendRequestExists(FriendRequest request);
    Task DeleteFriendRequest(FriendRequest request);
    Task<List<FriendRequest>> LoadAllFriendRequestsSentByPlayer(string sender);
    Task<List<FriendRequest>> LoadAllFriendRequestsSentToPlayer(string receiver);
}
