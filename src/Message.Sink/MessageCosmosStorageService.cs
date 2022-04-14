using Microsoft.Azure.Cosmos;

public class MessageCosmosStorageService : IMessageStorageService
{
    private readonly ILogger<MessageCosmosStorageService> _logger;

    private CosmosClient _cosmosClient;

    private static string TableName = "messages";
    private static string DatabaseName = "TablesDB";

    public MessageCosmosStorageService(ILogger<MessageCosmosStorageService> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
         AsyncHelper.RunAsync(async () => {
               await CreateIfNotExistsAsync(MessageCosmosStorageService.DatabaseName, MessageCosmosStorageService.TableName);
            });
    }

    public async void Initialize(string tableName)
    {
       AsyncHelper.RunAsync(async () => {
               await CreateIfNotExistsAsync(MessageCosmosStorageService.DatabaseName, MessageCosmosStorageService.TableName);
            });
    }

    public async Task<bool> CreateIfNotExistsAsync(string databaseName, string containerName)
        {
            _logger.LogInformation("Creating Database");
            try
            {
                var db = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
                if ( db.StatusCode == System.Net.HttpStatusCode.Created )
                {
                    ContainerProperties containerProperties = new ContainerProperties()
                    {
                        Id = containerName,
                        PartitionKeyPath = "/id",
                        IndexingPolicy = new IndexingPolicy()
                        {
                            Automatic = false,
                            IndexingMode = IndexingMode.Lazy,
                        }
                    };

                    var container = await db.Database.CreateContainerIfNotExistsAsync(containerProperties);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }                

            return await Task.FromResult<bool>(true);
        }

    public async Task<bool> SaveMessage(DeviceMessage message)
    {
        _logger.LogTrace($"Saving message in partition {message.Name} with rowkey {message.Id}");

        bool success = false;
        ItemResponse<DeviceMessage> response = null;

        try
        {
            var container = _cosmosClient.GetContainer(MessageCosmosStorageService.DatabaseName, MessageCosmosStorageService.TableName);

            if (container == null){
                _logger.LogCritical("Container was null");
                return false;
            }

            response = await container.CreateItemAsync<DeviceMessage>(message, new PartitionKey(message.Id));

            _logger.LogTrace("Insert of item consumed {0} request units", response.RequestCharge);

            if (response == null){
                _logger.LogCritical("Response was null");
                return false;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Created){
                success = true;
                _logger.LogDebug($"Saved message in partition {message.Name} with rowkey {message.Id}");
            }
            else{
                success = false;
                _logger.LogInformation($"Save of message in partition {message.Name} with rowkey {message.Id} resulted in {response.StatusCode}");
            }
        
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogInformation("Insert of item consumed {0} request units", response.RequestCharge);
            success = false;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            success = false;
        }

        
        return success;
    }
}