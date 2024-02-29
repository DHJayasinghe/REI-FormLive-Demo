using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace FormBuilder.Services;

public class RemoteDataFetcher(IConfiguration configuration)
{
    // Load from DB table
    private static readonly IEnumerable<DataMapEntry> mapping =
    [
        new("PM0007", "Office.ABN","Agency_ACN"),
        new("PM0007", "Office.LegalBusinessName","Agency_LicenceName"),
        new("PM0007", "Office.Name", "Agency_Name" ),
        new("PM0007", "Office.PhoneNumber", "Agency_Phone"),
        new("PM0007","Office.Postcode","Agency_Postcode"),
        new("PM0007","Office.Suburb","Agency_Suburb"),
        new("PM0007","Property.Postcode","Premises_Postcode"),
        new("PM0007","Property.Address1","Premises_Street1"),
        new("PM0007","Property.Address2","Premises_Street2"),
        new("PM0007","Property.Suburb","Premises_Suburb"),
        new("PM0007","Property.Tenant1","Tenant_NameFull"),
        new("PM0007","Property.Tenant2","Tenant2_NameFull"),
        new("PM0007","Property.Tenant3","Tenant3_NameFull"),
        new("PM0007", "Property.OfficeID.Relation", "Office.OfficeID" ),
        new("PM0002", "User.Email", "Agent_Email" ),
        new("PM0002", "User.FirstName", "Agent_Name" ),
        new("PM0002", "Office.UserID.Relation", "User.UserID"  ), // Define the relationship between Manager and Office
    ];

    private readonly IConfiguration _configuration = configuration;

    public string GenerateSQLQuery(string code, string whereClause)
    {
        var queryBuilder = new StringBuilder("SELECT ");

        bool hasMapping = false;
        foreach (var map in mapping.Where(map => map.Code == code && !map.Source.EndsWith("Relation")))
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
        foreach (var map in mapping.Where(map => map.Code == code && map.Source.EndsWith("Relation")))
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