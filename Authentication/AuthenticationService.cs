using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using player_service_net_test4.Models;
using player_service_net_test4.Services;

namespace player_service_net_test4.Authentication;
public interface IAuthenticationService
{
    Task<User?> GetUser(string? token);
}

public class AuthenticationService : MongoDbRepositoryBase, IAuthenticationService
{
    private readonly IW3CAuthenticationService _authenticationService;
    private readonly IWebsiteBackendRepository _websiteBackendRepository;

    public AuthenticationService(
        MongoClient mongoClient,
        IW3CAuthenticationService authenticationService,
        IWebsiteBackendRepository websiteBackendRepository
        ) : base(mongoClient)
    {
        _authenticationService = authenticationService;
        _websiteBackendRepository = websiteBackendRepository;
    }

    public async Task<User?> GetUser(string? token)
    {
        try
        {
            var user = _authenticationService.GetUserByToken(token);
            if (user == null) return null;
            var userDetails = await _websiteBackendRepository.GetPlayerProfile(user.BattleTag);
            if (userDetails == null) return null;
            return new User(user.BattleTag, userDetails.ProfilePicture);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
