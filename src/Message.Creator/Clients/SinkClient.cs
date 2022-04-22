using System.Net;

namespace Message.Creator.Clients
{
    public class SinkClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SinkClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<int> SendMessageAsync(DeviceMessage message)
        {
            var client = _httpClientFactory.CreateClient("Sink"); 

            var response = await client.PostAsJsonAsync("/api/message/receive", 
            message, 
            new System.Text.Json.JsonSerializerOptions(){
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });

            response.EnsureSuccessStatusCode();
            Console.WriteLine(response.StatusCode);

            return Convert.ToInt32(response.StatusCode);
        }
    }
}