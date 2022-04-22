using System.Text.Json.Serialization;
public class MessageResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("host")]
    public string Host { get; set; }
    [JsonPropertyName("status")]
    public MessageStatus Status { get; set; }
    [JsonPropertyName("sender")]
    public string Sender { get; set; }
    [JsonPropertyName("dependency")]
    public MessageResponse Dependency { get; set; }
}