﻿using Azure;
using Azure.Data.Tables;

namespace FormBuilder.Models.Domain;

public class MappingEntry : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string Code { get; private set; }
    public string Target { get; set; }
    public string Source { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    private MappingEntry() { }
    public MappingEntry(string clientId, string code, string source, string target) : this()
    {
        PartitionKey = clientId;
        RowKey = code + "-" + target;
        Code = code;
        Target = target;
        Source = source;
        Timestamp = DateTimeOffset.UtcNow;
    }
}