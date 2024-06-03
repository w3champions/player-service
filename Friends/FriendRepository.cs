using MongoDB.Driver;

namespace player_service_net_test4.Friends;

public class FriendRepository : MongoDbRepositoryBase, IFriendRepository
{
    public FriendRepository(MongoClient mongoClient) : base(mongoClient) {}

    public async Task<FriendList> LoadFriendList(string battleTag)
    {
        var friendList = await LoadFirst<FriendList>(battleTag);

        if (friendList == null)
        {
            friendList = new FriendList(battleTag);
            await Insert(friendList);
        }

        return friendList;
    }

    public Task UpsertFriendList(FriendList friendList)
    {
        return Upsert(friendList, p => p.Id == friendList.Id);
    }

    public async Task<FriendRequest> CreateFriendRequest(FriendRequest request)
    {
        await Insert(request);
        return request;
    }

    public async Task<FriendRequest> LoadFriendRequest(string sender, string receiver)
    {
        return await LoadFirst<FriendRequest>(r => r.Sender == sender && r.Receiver == receiver);
    }

    public async Task DeleteFriendRequest(FriendRequest request)
    {
        await Delete<FriendRequest>(r => r.Sender == request.Sender && r.Receiver == request.Receiver);
    }

    public async Task<bool> FriendRequestExists(FriendRequest request)
    {
        var req = await LoadFirst<FriendRequest>(r => r.Sender == request.Sender && r.Receiver == request.Receiver);
        if (req == null) return false;
        return true;
    }

    public async Task<List<FriendRequest>> LoadSentFriendRequests(string sender)
    {
        var requests = await LoadAll<FriendRequest>(r => r.Sender == sender);
        return requests;
    }

    public async Task<List<FriendRequest>> LoadReceivedFriendRequests(string receiver)
    {
        var requests = await LoadAll<FriendRequest>(r => r.Receiver == receiver);
        return requests;
    }
}
