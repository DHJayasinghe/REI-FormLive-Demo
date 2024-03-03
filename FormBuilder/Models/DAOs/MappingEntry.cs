using Azure;
using Azure.Data.Tables;

namespace FormBuilder.Models.DAOs;

public class MappingEntry : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string Code { get; set; }
    public string Target { get; set; }
    public string Source { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public MappingEntry() { }
    public MappingEntry(string clientId, string organizationId, string code, string source, string target) : this()
    {
        PartitionKey = clientId + ":" + organizationId;
        RowKey = code + "-" + target;
        Code = code;
        Target = target;
        Source = source;
        Timestamp = DateTimeOffset.UtcNow;
    }
}