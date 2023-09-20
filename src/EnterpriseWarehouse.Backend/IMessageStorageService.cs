using Azure.Data.Tables;

public interface IMessageStorageService
{
    Task SaveOrderAsync(int repairPartId);
}