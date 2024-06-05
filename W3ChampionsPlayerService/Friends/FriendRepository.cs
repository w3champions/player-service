using MongoDB.Driver;

namespace W3ChampionsPlayerService.Friends;

public class FriendRepository(MongoClient mongoClient) : MongoDbRepositoryBase(mongoClient), IFriendRepository
{
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

    public async Task<FriendRequest> CreateFriendRequest(FriendRequest req)
    {
        await Insert(req);
        return req;
    }

    public async Task<FriendRequest> LoadFriendRequest(FriendRequest req)
    {
        return await LoadFirst<FriendRequest>(r => r.Sender == req.Sender && r.Receiver == req.Receiver);
    }

    public async Task DeleteFriendRequest(FriendRequest req)
    {
        await Delete<FriendRequest>(r => r.Sender == req.Sender && r.Receiver == req.Receiver);
    }

    public async Task<bool> FriendRequestExists(FriendRequest req)
    {
        var request = await LoadFirst<FriendRequest>(r => r.Sender == req.Sender && r.Receiver == req.Receiver);
        if (request == null) return false;
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
