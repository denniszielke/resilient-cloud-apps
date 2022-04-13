using Azure.Data.Tables;

public class MessageStorageService : IMessageStorageService
{
    private readonly ILogger<MessageStorageService> _logger;
    private TableServiceClient _tableServiceClient;
    private TableClient _tableClient;

    private static string TableName = "messages";

    public MessageStorageService(ILogger<MessageStorageService> logger, TableServiceClient tableServiceClient)
    {
        _logger = logger;
        _tableServiceClient = tableServiceClient;
        AsyncHelper.RunAsync(async () => {
               await CreateIfNotExistsAsync(MessageStorageService.TableName);
        });
    }

    public async void Initialize(string tableName)
    {
       AsyncHelper.RunAsync(async () => {
               await CreateIfNotExistsAsync(MessageStorageService.TableName);
        });
    }

    public async Task<bool> CreateIfNotExistsAsync(string tableName)
    {
        var item = await _tableServiceClient.CreateTableIfNotExistsAsync(tableName);
        if (item!=null && item.Value != null && !item.GetRawResponse().IsError){
            _tableClient = _tableServiceClient.GetTableClient(tableName);
            await _tableClient.CreateIfNotExistsAsync();
            return await Task.FromResult<bool>(true);
        }
        else
        {
            _tableClient = _tableServiceClient.GetTableClient(tableName);
            var table = await _tableClient.CreateIfNotExistsAsync();
            if (table != null  && table.Value != null && !table.GetRawResponse().IsError)
            {
                return await Task.FromResult<bool>(true);
            }

            if (_tableClient != null)
            return await Task.FromResult<bool>(true);
        }
        
        return await Task.FromResult<bool>(false);
    }

    public async Task<bool> SaveMessage(DeviceMessage message)
    {
        _logger.LogTrace($"Saving message in partition {message.Name} with rowkey {message.Id}");

        TableEntity entity = new TableEntity(message.Name, message.Id){
            { "Humidity", message.Humidity},
            { "Temperature", message.Temperature},
            { "Message", message.Message}
        };

        bool success = false;

        try
        {
            var itemAdded = await _tableClient.AddEntityAsync(entity);
            success = !itemAdded.IsError;
            _logger.LogDebug($"Saved match in partition {message.Name} with rowkey {message.Id}");

        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        
        return success;
    }
}