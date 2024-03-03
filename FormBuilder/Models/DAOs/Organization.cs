using Azure;
using Azure.Data.Tables;

namespace FormBuilder.Models.DAOs;

public class Organization : ITableEntity
{
    public string Id { get; set; }
    public string ClientId { get; set; }
    public string State { get; set; }
    public string Token { get; set; }

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public Organization() { }
    public Organization(string id, string clientId, string state, string token) : this()
    {
        ClientId = clientId;
        State = state;
        Token = token;
        PartitionKey = clientId;
        RowKey = id;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
