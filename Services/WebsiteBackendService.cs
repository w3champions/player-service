using player_service_net_test4.Models;
using Newtonsoft.Json;

namespace player_service_net_test4.Services
{
    public interface IWebsiteBackendRepository
    {
        Task<User?> GetUser(string battleTag);
    }

    public class WebsiteBackendRepository : IWebsiteBackendRepository
    {
        private static readonly string StatisticServiceApiUrl = Environment.GetEnvironmentVariable("STATISTIC_SERVICE_URI") ?? "";

        public async Task<User?> GetUser(string battleTag)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(StatisticServiceApiUrl);
                var escapeDataString = Uri.EscapeDataString(battleTag);
                var result = await httpClient.GetAsync($"/api/players/{escapeDataString}/user-brief");
                if (!result.IsSuccessStatusCode) {
                    return null;
                }
                var content = await result.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<User>(content);
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
