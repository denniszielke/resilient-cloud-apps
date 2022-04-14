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

    public async Task<MessageStatus> SaveMessage(DeviceMessage message)
    {
        _logger.LogTrace($"Saving message in partition {message.Name} with rowkey {message.Id}");

        MessageStatus status = MessageStatus.Failed;

        if (message == null || string.IsNullOrWhiteSpace(message.Name) || string.IsNullOrWhiteSpace(message.Id)) 
        {
            return status;
        }

        try
        {
            
            TableEntity existingEntity = await _tableClient.GetEntityAsync<TableEntity>(message.Name, message.Id);
            if ( existingEntity != null)
            {
                existingEntity["Humidity"] = message.Humidity;
                existingEntity["Temperature"] = message.Temperature;
                existingEntity["Message"] = message.Message;

                Task<Azure.Response> response = _tableClient.UpdateEntityAsync<TableEntity>(existingEntity, existingEntity.ETag, TableUpdateMode.Replace);

                _logger.LogDebug($"Updated message in partition {message.Name} with rowkey {message.Id}");
                status = MessageStatus.Ok;
                return status;
            }   
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            status = MessageStatus.Failed;
            _logger.LogInformation("Update of item failed. {0}", ex.Message);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 429)
        {
            status = MessageStatus.Throttled;
            _logger.LogInformation("Please slow down. {0}", ex.Message);
            
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"Failed to update message in partition {message.Name} with rowkey {message.Id}", ex.Message);
        }

        try
        {
            
            TableEntity entity = new TableEntity(message.Name, message.Id){
                { "Humidity", message.Humidity},
                { "Temperature", message.Temperature},
                { "Message", message.Message}
            };

            var itemAdded = await _tableClient.AddEntityAsync<TableEntity>(entity);
            if(!itemAdded.IsError ){
                status = MessageStatus.Ok;
                _logger.LogDebug($"Saved message in partition {message.Name} with rowkey {message.Id}");
            }else
            {
                status = MessageStatus.Failed;
                _logger.LogInformation($"Failed to add message in partition {message.Name} with rowkey {message.Id}");
            }          
                
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {            
            status = MessageStatus.Failed;
            _logger.LogError($"Failed to add message in partition {message.Name} with rowkey {message.Id}", ex.Message);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 429)
        {
            _logger.LogInformation("Please slow down. {0}", ex.Message);
            status = MessageStatus.Throttled;
            _logger.LogError($"Throttled to add message in partition {message.Name} with rowkey {message.Id}", ex.Message);
        }
        catch (System.Exception ex)
        {
            status = MessageStatus.Failed;
            _logger.LogError($"Failed to add message in partition {message.Name} with rowkey {message.Id}", ex.Message);
        }

        
        return status;
    }
}