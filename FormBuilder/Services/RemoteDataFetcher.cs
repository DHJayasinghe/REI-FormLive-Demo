using Azure.Data.Tables;
using Dapper;
using FormBuilder.Models.Domain;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace FormBuilder.Services;

public class RemoteDataFetcher(TableServiceClient tableServiceClient)
{
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
            queryBuilder.Append($" WHERE {whereClause}");

        return queryBuilder.ToString();
    }

    public async Task<List<TResult>> QueryAsync<TResult>(string clientId, string query, object parameters)
    {
        var table = tableServiceClient.GetTableClient("Client");
        var connectionString = (table.GetEntity<Client>(clientId, clientId)).Value.ConnectionString;

        using IDbConnection conn = new SqlConnection(connectionString);
        return (await conn.QueryAsync<TResult>(query, parameters, commandType: CommandType.Text, commandTimeout: (int)TimeSpan.FromMinutes(2).TotalSeconds)).ToList();
    }

}