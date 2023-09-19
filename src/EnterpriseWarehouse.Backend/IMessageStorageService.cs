using Azure.Data.Tables;

public interface IMessageStorageService
{
    void Initialize(string tableName);

    Task<MessageStatus> SaveMessage(DeviceMessage message);
}