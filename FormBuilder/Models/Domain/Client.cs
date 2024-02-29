using Azure;
using Azure.Data.Tables;

namespace FormBuilder.Models.Domain;

public class Client : ITableEntity
{
    public string Name { get; set; }
    public string ConnectionString { get; set; }
    public string ApiKey { get; set; }

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    private Client() { }
    public Client(string name, string connectionString) : this()
    {
        Name = name;
        ConnectionString = connectionString;
        ApiKey = Guid.NewGuid().ToString();
        PartitionKey = ApiKey.Replace("-", "");
        RowKey = PartitionKey;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
