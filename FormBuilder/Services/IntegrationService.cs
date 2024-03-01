using Azure.Data.Tables;
using FormBuilder.Models.Domain;
using FormBuilder.Models.DTO;
using Newtonsoft.Json;
using System.Dynamic;
using System.Text;

namespace FormBuilder.Services;

public class IntegrationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, DataAccessService dataAccessService, RemoteDataFetcher dataloadedService)
{
    public async Task<IEnumerable<UserTemplate>> GetTemplatesAsync(string clientId, string organizationId)
    {
        using var _client = await GetApiClientAsync(clientId, organizationId);
        var response = await _client.GetAsync("/user-templates");
        return await response.Content.ReadFromJsonAsync<IEnumerable<UserTemplate>>();
    }

    private async Task<HttpClient> GetApiClientAsync(string clientId, string organizationId)
    {
        var _client = httpClientFactory.CreateClient("ReiFormsLive");
        var token = (await dataAccessService.GetOrganizationAsync(clientId, organizationId)).Token;

        var deploperApiToken = $"{configuration.GetValue<string>("DeveloperApiKey")}:{token}";
        _client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(deploperApiToken)}");

        return _client;
    }

    private static string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    private static async Task<string> CreateUserSessionAsync(HttpClient _client)
    {
        var createSessionResponse = await _client.PostAsync("/user/session", new StringContent("", Encoding.UTF8, "application/json"));
        createSessionResponse.EnsureSuccessStatusCode();

        var createSessionResult = await createSessionResponse.Content.ReadFromJsonAsync<SessionPostResponse>();
        var token = createSessionResult.Token;
        return token;
    }

    public async Task<string> CreateFormAsync(string clientId, string organizationId, int id, string name, string code, Dictionary<string, object> parameters)
    {
        using var apiClient = await GetApiClientAsync(clientId, organizationId);
        var formId = (await (await apiClient.PostAsJsonAsync($"/user-templates/{id}/form", new
        {
            name,
            isPrivate = false
        }))
        .EnsureSuccessStatusCode()
        .Content.ReadFromJsonAsync<FormPostResponse>())
        .Id;

        await FillFormAsync(apiClient, clientId, formId, code, parameters);
        string token = await CreateUserSessionAsync(apiClient);

        var portalUrl = (await dataAccessService.GetOrganizationAsync(clientId, organizationId)).PortalUrl;

        // configuration["PortalUrl"] -> should goes to client
        var url = $"{portalUrl}/?token={token}#form/{formId}/display";

        return url;
    }

    private async Task FillFormAsync(HttpClient apiClient, string clientId, int formId, string code, Dictionary<string, object> parameters)
    {
        dynamic requestObject = new ExpandoObject();
        var requestDictionary = (IDictionary<string, object>)requestObject;

        foreach (var kvp in parameters)
            requestDictionary[kvp.Key] = kvp.Value.ToString();

        var query = await dataloadedService.GenerateSQLQueryAsync(clientId, code);
        var data = (await dataloadedService.QueryAsync<object>(clientId, query, requestObject as object)).First();
        var fillFormResponse = await apiClient.PutAsync($"/forms/{formId}/save", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
        fillFormResponse.EnsureSuccessStatusCode();
    }
}

public static class IntegrationServiceExtension
{
    public static IServiceCollection RegisterIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => new TableServiceClient(configuration.GetConnectionString("ConfigDb")).CreateSchemaIfNotExist());
        services.AddHttpContextAccessor();
        services.AddHttpClient("ReiFormsLive", client =>
        {
            client.BaseAddress = new Uri(configuration["DeveloperApiBaseUrl"].ToString());
        });
        services.AddSingleton<DataAccessService>();
        services.AddSingleton<IntegrationService>();
        services.AddSingleton<RemoteDataFetcher>();
        services.AddScoped<Identity>();
        return services;
    }

    public static TableServiceClient CreateSchemaIfNotExist(this TableServiceClient tableServiceClient)
    {
        tableServiceClient.GetTableClient("Client").CreateIfNotExists();
        tableServiceClient.GetTableClient("Mapping").CreateIfNotExists();
        tableServiceClient.GetTableClient("Organization").CreateIfNotExists();
        return tableServiceClient;
    }
}
