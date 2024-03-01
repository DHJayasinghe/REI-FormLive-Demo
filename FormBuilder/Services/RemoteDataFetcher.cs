using Azure.Data.Tables;
using Dapper;
using FormBuilder.Models.Domain;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Data;
using System.Text;

namespace FormBuilder.Services;

public class RemoteDataFetcher(IConfiguration configuration, TableServiceClient tableServiceClient)
{
    private readonly IConfiguration _configuration = configuration;

    public string GenerateSQLQuery(string clientId, string code)
    {
        var queryBuilder = new StringBuilder("SELECT ");

        var table = tableServiceClient.GetTableClient("Mapping");
        var mapping = new List<MappingEntry>();
        var queryEntries = table.Query<MappingEntry>(d => d.PartitionKey == clientId);
        foreach (var entry in queryEntries)
        {
            mapping.Add(entry);
        }

        bool hasMapping = false;
        foreach (var map in mapping.Where(map => map.Code == code && !map.Source.EndsWith("Relation") && !map.Source.StartsWith("WhereClause")))
        {
            hasMapping = true;
            string[] parts = map.Source.Split('.');
            string tableName = parts[0];
            string columnName = parts[1];

            queryBuilder.Append($"[{tableName}].[{columnName}] AS {map.Target}, ");
        }
        if (!hasMapping) return string.Empty;

        queryBuilder
            .Remove(queryBuilder.Length - 2, 2) // Remove the trailing comma and space
            .Append(" FROM ");

        // Check if there's a relationship defined for this table
        foreach (var map in mapping.Where(map => map.Code == code && map.Source.EndsWith("Relation") && !map.Source.StartsWith("WhereClause")))
        {
            string[] leftTableAndKeyColumn = map.Source.Replace(".Relation", "").Split('.');
            string leftTableName = leftTableAndKeyColumn[0];
            string leftTableKeyColumnName = leftTableAndKeyColumn[1];

            string[] rightTableAndKeyColumn = map.Target.Split(".");
            string rightTableName = rightTableAndKeyColumn[0];
            string rightTableKeyColumnName = rightTableAndKeyColumn[1];

            queryBuilder.Append($"[{leftTableName}] LEFT JOIN [{rightTableName}]");
            queryBuilder.Append($" ON [{leftTableName}].{leftTableKeyColumnName} = [{rightTableName}].{rightTableKeyColumnName}");
        }

        var whereClause = mapping.Where(map => map.Code == code && map.Source.StartsWith("WhereClause")).FirstOrDefault()?.Target;
        if (!string.IsNullOrEmpty(whereClause))
        {
            queryBuilder.Append($" WHERE {whereClause}");
        }

        return queryBuilder.ToString();
    }

    private SqlConnection Connection => new(_configuration.GetConnectionString("SampleDb"));

    public async Task<List<TResult>> QueryAsync<TResult>(string spName, object parameters = null)
    {
        using IDbConnection conn = Connection;
        return (await conn.QueryAsync<TResult>(spName, parameters, commandType: CommandType.Text, commandTimeout: (int)TimeSpan.FromMinutes(2).TotalSeconds)).ToList();
    }

}

public record DataMapEntry(string Code, string Source, string Target);