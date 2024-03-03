using Dapper;
using FormBuilder.Models.DAOs;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace FormBuilder.Services;

public class RemoteDataFetcher(DataAccessService dataAccessService)
{
    public async Task<string> GenerateSQLQueryAsync(string clientId,string organizationId, string code)
    {
        var mappings = await dataAccessService.GetMappingsAsync(clientId, organizationId);
        if (!mappings.Any()) return string.Empty;

        var queryBuilder = new StringBuilder();

        AppendSelectedColumns(code, mappings, queryBuilder);
        AppendRelationJoins(code, queryBuilder, mappings);
        AppendWhereClauses(code, queryBuilder, mappings);

        return queryBuilder.ToString();
    }

    private static void AppendSelectedColumns(string code, IEnumerable<MappingEntry> mappings, StringBuilder queryBuilder)
    {
        queryBuilder.Append("SELECT ");
        foreach (var map in mappings.Where(map => map.Code == code && !map.Source.EndsWith("Relation") && !map.Source.StartsWith("WhereClause")))
        {
            string[] parts = map.Source.Split('.');
            string tableName = parts[0];
            string columnName = parts[1];

            queryBuilder.Append($"[{tableName}].[{columnName}] AS {map.Target}, ");
        }
        // Remove the trailing comma and space
        queryBuilder.Remove(queryBuilder.Length - 2, 2);
    }

    private static void AppendWhereClauses(string code, StringBuilder queryBuilder, IEnumerable<MappingEntry> mapping)
    {
        var whereClause = mapping.Where(map => map.Code == code && map.Source.StartsWith("WhereClause")).FirstOrDefault()?.Target;
        if (!string.IsNullOrEmpty(whereClause))
            queryBuilder.Append($" WHERE {whereClause}");
    }

    private static void AppendRelationJoins(string code, StringBuilder queryBuilder, IEnumerable<MappingEntry> mapping)
    {
        queryBuilder.Append(" FROM ");
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
    }

    public async Task<List<TResult>> QueryAsync<TResult>(string clientId, string query, object parameters)
    {
        var connectionString = (await dataAccessService.GetClientAsync(clientId)).ConnectionString;

        using IDbConnection conn = new SqlConnection(connectionString);
        return (await conn.QueryAsync<TResult>(query, parameters, commandType: CommandType.Text, commandTimeout: (int)TimeSpan.FromMinutes(2).TotalSeconds)).ToList();
    }
}