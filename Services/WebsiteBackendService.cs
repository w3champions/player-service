using player_service.Models;
using Newtonsoft.Json;

namespace player_service.Services
{
    public class WebsiteBackendService
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

        public async Task<List<User>?> GetUsers(List<string> battleTags)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(StatisticServiceApiUrl);
                var escapeDataString = string.Join(",", battleTags.Select(x => x.Replace("#", "%23")));
                var result = await httpClient.GetAsync($"/api/players/{escapeDataString}/user-brief/many");
                if (!result.IsSuccessStatusCode) {
                    return null;
                }
                var content = await result.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<List<User>>(content);
                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
