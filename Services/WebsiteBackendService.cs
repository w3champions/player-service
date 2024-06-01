using player_service_net_test4.Models;
using Newtonsoft.Json;

namespace player_service_net_test4.Services
{
    public interface IWebsiteBackendRepository
    {
        Task<PlayerProfile?> GetPlayerProfile(string battleTag);
    }

    public class WebsiteBackendRepository : IWebsiteBackendRepository
    {
        private static readonly string StatisticServiceApiUrl = Environment.GetEnvironmentVariable("STATISTIC_SERVICE_URI") ?? "";

        public async Task<PlayerProfile?> GetPlayerProfile(string battleTag)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(StatisticServiceApiUrl);
                var escapeDataString = Uri.EscapeDataString(battleTag);
                var result = await httpClient.GetAsync($"/api/players/{escapeDataString}/clan-and-picture");
                var content = await result.Content.ReadAsStringAsync();
                var userDetails = JsonConvert.DeserializeObject<PlayerProfile>(content);
                return userDetails;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }

    public class PlayerProfile
    {
        public string? ClanId { get; set; }
        public required ProfilePicture ProfilePicture { get; set;}
    }
}
