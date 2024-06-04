using MongoDB.Driver;

namespace player_service_net_test4.Friends;

public class FriendRequestCache(MongoClient mongoClient) : MongoDbRepositoryBase(mongoClient)
{
    private List<FriendRequest> _requests = new List<FriendRequest>();
    public Dictionary<string, List<FriendRequest>> _requestsBySender = new Dictionary<string, List<FriendRequest>>();
    public Dictionary<string, List<FriendRequest>> _requestsByReceiver = new Dictionary<string, List<FriendRequest>>();
    private Object _lock = new Object();

    public async Task<List<FriendRequest>> LoadAllFriendRequests()
    {
        await UpdateCacheIfNeeded();
        return _requests;
    }

    public async Task<List<FriendRequest>> LoadSentFriendRequests(string sender)
    {
        await UpdateCacheIfNeeded();
        return _requests.Where(x => x.Sender == sender).ToList();
    }

    public async Task<List<FriendRequest>> LoadReceivedFriendRequests(string receiver)
    {
        await UpdateCacheIfNeeded();
        return _requests.Where(x => x.Receiver == receiver).ToList();
    }

    public async Task<FriendRequest?> LoadFriendRequest(string sender, string receiver)
    {
        await UpdateCacheIfNeeded();
        return _requests.SingleOrDefault(x => x.Sender == sender && x.Receiver == receiver);
    }

    public async Task<FriendRequest?> LoadFriendRequest2(FriendRequest req)
    {
        await UpdateCacheIfNeeded();
        return _requests.SingleOrDefault(x => x.Sender == req.Sender && x.Receiver == req.Receiver);
    }

    public async Task<bool> FriendRequestExists(FriendRequest req)
    {
        await UpdateCacheIfNeeded();
        return _requests.SingleOrDefault(x => x.Sender == req.Sender && x.Receiver == req.Receiver) != null;
    }

    // public async Task<List<FriendRequest>> LoadFriendRequestsBySender(string sender)
    // {
    //     await UpdateCacheIfNeeded();
    //     try
    //     {
    //         return _requestsBySender[sender];
    //     }
    //     catch (KeyNotFoundException)
    //     {
    //         return [];
    //     }
    // }

    // public async Task<List<FriendRequest>> LoadFriendRequestsByReceiver(string receiver)
    // {
    //     await UpdateCacheIfNeeded();
    //     try
    //     {
    //         return _requestsByReceiver[receiver];
    //     }
    //     catch(KeyNotFoundException)
    //     {
    //         return [];
    //     }
    // }

    public void Insert(FriendRequest req)
    {
        lock (_lock)
        {
            _requests = _requests.Append(req).ToList();
        }
    }

    public void Delete(FriendRequest req)
    {
        lock (_lock)
        {
            _requests = _requests.Where(m => m.Sender != req.Sender && m.Receiver != req.Receiver).ToList();
        }
    }

    private async Task UpdateCacheIfNeeded()
    {
        if (_requests.Count == 0)
        {
            var mongoCollection = CreateCollection<FriendRequest>();
            _requests = await mongoCollection.Find(r => true).ToListAsync();
        }
        // _requestsBySender = MapRequestsBySender();
        // _requestsByReceiver = MapRequestsByReceiver();
    }

    public Dictionary<string, List<FriendRequest>> MapRequestsBySender()
    {
        return _requests
            .GroupBy(x => x.Sender)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );
    }

    public Dictionary<string, List<FriendRequest>> MapRequestsByReceiver()
    {
        return _requests
            .GroupBy(x => x.Receiver)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );
    }
}
