using System.Net;
using System.Text.Json;

namespace Message.Receiver.Clients
{
    public class SinkClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SinkClient> _logger;

        public SinkClient(IHttpClientFactory httpClientFactory, ILogger<SinkClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<MessageResponse> SendMessageAsync(DeviceMessage message)
        {
            var client = _httpClientFactory.CreateClient("Sink"); 
            MessageResponse receivedResponse = null;
            var response = await client.PostAsJsonAsync("/api/message/receive", 
            message, 
            new System.Text.Json.JsonSerializerOptions(){
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });

            try
            {
                Console.WriteLine(response.StatusCode);

                if(response.IsSuccessStatusCode){
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseBody);
                    var sinkResponse = JsonSerializer.Deserialize<MessageResponse>(responseBody)!;
                    receivedResponse = new MessageResponse(){
                        Id = message.Id, Status = MessageStatus.Ok, Sender = "message-receiver", Host = Environment.MachineName
                    };
                    receivedResponse.Dependency = sinkResponse;
                }
                else{
                    if (response.StatusCode == HttpStatusCode.TooManyRequests){
                            receivedResponse = new MessageResponse(){
                            Id = message.Id, Status = MessageStatus.Throttled, Sender = "message-receiver", Host = Environment.MachineName
                        };
                    }else{
                        receivedResponse = new MessageResponse(){
                            Id = message.Id, Status = MessageStatus.Failed, Sender = "message-receiver", Host = Environment.MachineName
                        };
                    }                    
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                receivedResponse = new MessageResponse(){
                        Id = message.Id, Status = MessageStatus.Failed, Sender = "message-receiver", Host = Environment.MachineName
                    };
            }

            return receivedResponse;
        }
    }
}