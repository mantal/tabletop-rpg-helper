using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RPG.Services
{
	public class Discord
	{
		private const string WebhookUrl = "NO";
		
		private readonly HttpClient _httpClient;

		public Discord()
		{
			_httpClient = new HttpClient();
		}

		public async Task Send(string message, string username)
		{
			await _httpClient.PostAsync(WebhookUrl, 
										 new StringContent($"{{\"content\": \"{message}\", \"username\": \"{username}\"}}",
											 Encoding.UTF8,
											 "application/json")
				);
			
		}
	}
}