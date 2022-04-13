using Azure.Data.Tables;

public interface IMessageStorageService
{
    void Initialize(string tableName);

    Task<bool> SaveMessage(DeviceMessage message);
}