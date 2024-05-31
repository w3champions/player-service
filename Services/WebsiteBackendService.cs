using System.Text.Json;
using player_service_net_test4.Models;

namespace player_service_net_test4.Services
{
    public interface IWebsiteBackendRepository
    {
        Task<PlayerProfile?> GetPlayerProfile(string battleTag);
    }

    public class WebsiteBackendRepository : IWebsiteBackendRepository
    {
        private static readonly string StatisticServiceApiUrl = Environment.GetEnvironmentVariable("STATISTIC_SERVICE_URI") ?? "http://localhost:5000";

        public async Task<PlayerProfile?> GetPlayerProfile(string battleTag)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(StatisticServiceApiUrl);
            var escapeDataString = Uri.EscapeDataString(battleTag);
            var result = await httpClient.GetAsync($"/api/players/{escapeDataString}");
            var content = await result.Content.ReadAsStringAsync();
            var userDetails = JsonSerializer.Deserialize<PlayerProfile>(content);
            return userDetails;
        }
    }

    public class PlayerProfile
    {
        public string? ClanId { get; set; }
        public required ProfilePicture ProfilePicture { get; set;}
    }
}
