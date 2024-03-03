using Azure.Data.Tables;
using FormBuilder.Models.DAOs;

namespace FormBuilder.Services;

public class DataAccessService(TableServiceClient tableServiceClient)
{
    //TODO: Caching Required Here!!!!

    public async Task<Organization> GetOrganizationAsync(string clientId, string organizationId)
    {
        var tableClient = tableServiceClient.GetTableClient("Organization");
        return (await tableClient.GetEntityAsync<Organization>(clientId, organizationId)).Value;
    }

    public async Task<Client> GetClientAsync(string clientId)
    {
        var table = tableServiceClient.GetTableClient("Client");
        return (await table.GetEntityAsync<Client>(clientId, "NA")).Value;
    }

    public async Task<IEnumerable<MappingEntry>> GetMappingsAsync(string clientId, string organizationId)
    {
        var table = tableServiceClient.GetTableClient("Mapping");
        var partitionKey = clientId + ":" + organizationId;

        var queryEntries = table.Query<MappingEntry>(d => d.PartitionKey == partitionKey);
        return await Task.FromResult(queryEntries.Select(entry => entry));
    }
}
