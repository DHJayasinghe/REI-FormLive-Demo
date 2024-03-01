using Azure.Data.Tables;
using FormBuilder.Models.Domain;

namespace FormBuilder.Services;

public class DataAccessService(TableServiceClient tableServiceClient)
{
    public async Task<Organization> GetOrganizationAsync(string clientId, string organizationId)
    {
        var tableClient = tableServiceClient.GetTableClient("Organization");
        return (await tableClient.GetEntityAsync<Organization>(clientId, organizationId)).Value;
    }

    public async Task<Client> GetClientAsync(string clientId)
    {
        var table = tableServiceClient.GetTableClient("Client");
        var partitionKey = clientId.Replace("-", "");

        return (await table.GetEntityAsync<Client>(partitionKey, partitionKey)).Value;
    }

    public async Task<IEnumerable<MappingEntry>> GetMappingsAsync(string clientId)
    {
        var table = tableServiceClient.GetTableClient("Mapping");
        var partitionKey = clientId.Replace("-", "");

        var queryEntries = table.Query<MappingEntry>(d => d.PartitionKey == partitionKey);
        return await Task.FromResult(queryEntries.Select(entry => entry));
    }
}
