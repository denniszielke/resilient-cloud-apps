using Microsoft.AspNetCore.Mvc;
using Contonance.WebPortal.Shared;
using Azure.AI.OpenAI;
using Azure;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualBasic;

namespace Contonance.WebPortal.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<KnowledgeBaseController> _logger;
    private readonly string _deployedModelName;
    private readonly string _searchServiceEndpoint;
    private readonly string _searchServiceKey;
    private readonly string _searchIndexName;
    private readonly string _blobSasToken;
    private readonly string _blobContainerUrl;

    public KnowledgeBaseController(OpenAIClient openAIClient, IConfiguration config, ILogger<KnowledgeBaseController> logger)
    {
        _openAIClient = openAIClient;
        _logger = logger;

        var deployedModelName = config["AzureOpenAiDeployment"];
        ArgumentException.ThrowIfNullOrEmpty(deployedModelName);
        _deployedModelName = deployedModelName;

        var searchServiceEndpoint = config["AzureCognitiveSearchServiceEndpoint"];
        ArgumentException.ThrowIfNullOrEmpty(searchServiceEndpoint);
        _searchServiceEndpoint = searchServiceEndpoint;

        var searchServiceKey = config["AzureCognitiveSearchKey"];
        ArgumentException.ThrowIfNullOrEmpty(searchServiceKey);
        _searchServiceKey = searchServiceKey;

        var searchIndexName = config["AzureCognitiveSearchIndexName"];
        ArgumentException.ThrowIfNullOrEmpty(searchIndexName);
        _searchIndexName = searchIndexName;

        var blobSasToken = config["AzureBlobSasToken"];
        ArgumentException.ThrowIfNullOrEmpty(blobSasToken);
        _blobSasToken = blobSasToken;

        var blobContainerUrl = config["AzureBlobContainerUrl"];
        ArgumentException.ThrowIfNullOrEmpty(blobContainerUrl);
        _blobContainerUrl = blobContainerUrl;
    }

    [HttpPost]
    public async Task<KnowledgeBaseResponse> GetResults([FromBody] string questionFromUser)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                new ChatMessage(ChatRole.System, "You are an AI assisted knowledge base designed to help users extract information for building and repairing sailboat from retrieved English documents. Please scrutinize the English documents carefully before formulating a response."),
                new ChatMessage(ChatRole.User, questionFromUser)
            },
            AzureExtensionsOptions = new AzureChatExtensionsOptions()
            {
                Extensions =
                {
                    new AzureCognitiveSearchChatExtensionConfiguration()
                    {
                        SearchEndpoint = new Uri(_searchServiceEndpoint),
                        IndexName = _searchIndexName,
                        SearchKey = new AzureKeyCredential(_searchServiceKey),
                        SemanticConfiguration = "default",
                        QueryType = AzureCognitiveSearchQueryType.Semantic
                    }
                }
            }
        };

        var chatCompletionsResponse = await _openAIClient.GetChatCompletionsAsync(_deployedModelName, chatCompletionsOptions);

        var message = chatCompletionsResponse.Value.Choices[0].Message;

        var contextMessage = message.AzureExtensionsContext.Messages[0].Content;
        var chatExtensionContextMessage = JsonSerializer.Deserialize<ChatExtensionContextMessage>(contextMessage, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var kbResponse = new KnowledgeBaseResponse
        {
            Question = questionFromUser,
            Answer = message.Content,
            Citations = chatExtensionContextMessage!.citations.Select((_, i) => new KBCitation
            {
                Id = i + 1,
                Title = _.title!,
                Filepath = _.filepath!,
                Url = $"{_blobContainerUrl}/{_.filepath}?{_blobSasToken}"
            }).ToList()
        };

        var initialCount = kbResponse.Citations.Count;
        for (int i = 1; i <= initialCount; i++)
        {
            if (kbResponse.Answer.Contains($"[doc{i}]"))
            {
                var refCitation = kbResponse.Citations.Single(_ => _.Id == i);
                var duplicates = kbResponse.Citations.Where(_ => _ != refCitation && _.Filepath == refCitation.Filepath).ToList();
                foreach (var duplicate in duplicates)
                {
                    kbResponse.Answer = kbResponse.Answer.Replace($"[doc{duplicate.Id}]", $"[doc{refCitation.Id}]");
                    kbResponse.Citations.Remove(duplicate);
                }
            }
            else
            {
                kbResponse.Citations.RemoveAll(_ => _.Id == i);
            }
        }

        return kbResponse;
    }
}
