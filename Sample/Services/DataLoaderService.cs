using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text;

namespace Sample.Services;

public class DataLoaderService(IConfiguration configuration)
{
    // Load from DB table
    private static readonly Dictionary<string, string> mapping = new()
    {
        { "Office.Address", "Agency_AddressFull" },
        { "Office.Name", "Agency_Name" },
        { "Office.PhoneNumber", "Agency_Phone" },
        { "User.Email", "Agent_Email" },
        { "User.FirstName", "Agent_Name" },
        { "Office.UserID.Relation", "User.UserID"  } // Define the relationship between Manager and Office
    };

    private readonly IConfiguration _configuration = configuration;

    public string GenerateSQLQuery(string whereClause)
    {
        var queryBuilder = new StringBuilder("SELECT ");

        foreach (var map in mapping.Where(map => !map.Key.EndsWith("Relation")))
        {
            string[] parts = map.Key.Split('.');
            string tableName = parts[0];
            string columnName = parts[1];

            queryBuilder.Append($"[{columnName}] AS {map.Value}, ");
        }

        queryBuilder
            .Remove(queryBuilder.Length - 2, 2) // Remove the trailing comma and space
            .Append(" FROM ");

        // Check if there's a relationship defined for this table
        foreach (var map in mapping.Where(map => map.Key.EndsWith("Relation")))
        {
            string[] leftTableAndKeyColumn = map.Key.Replace(".Relation", "").Split('.');
            string leftTableName = leftTableAndKeyColumn[0];
            string leftTableKeyColumnName = leftTableAndKeyColumn[1];

            string[] rightTableAndKeyColumn = map.Value.Split(".");
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

public class FormLiveData
{
    public string Agency_AddressFull { get; set; }
    public string Agency_Name { get; set; }
    public string Agency_Phone { get; set; }
    public string Agent_Email { get; set; }
    public string Agent_Name { get; set; }
}
