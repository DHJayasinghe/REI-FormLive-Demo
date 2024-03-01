using Azure.Data.Tables;
using FormBuilder.Models.Domain;
using FormBuilder.Models.DTO;
using Newtonsoft.Json;
using System.Dynamic;
using System.Text;

namespace FormBuilder.Services;

public class IntegrationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, RemoteDataFetcher dataloadedService)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("ReiFormsLive");

    public async Task<IEnumerable<UserTemplate>> GetTemplatesAsync()
    {
        var response = await _client.GetAsync("/user-templates");
        return await response.Content.ReadFromJsonAsync<IEnumerable<UserTemplate>>();
    }

    private static async Task<string> CreateUserSessionAsync(HttpClient _client)
    {
        var createSessionResponse = await _client.PostAsync("/user/session", new StringContent("", Encoding.UTF8, "application/json"));
        createSessionResponse.EnsureSuccessStatusCode();

        var createSessionResult = await createSessionResponse.Content.ReadFromJsonAsync<SessionPostResponse>();
        var token = createSessionResult.Token;
        return token;
    }

    public async Task<string> CreateFormAsync(string clientId, int id, string name, string code, Dictionary<string, object> parameters)
    {
        var formId = (await (await _client.PostAsJsonAsync($"/user-templates/{id}/form", new
        {
            name,
            isPrivate = false
        }))
        .EnsureSuccessStatusCode()
        .Content.ReadFromJsonAsync<FormPostResponse>())
        .Id;

        await FillFormAsync(clientId, formId, code, parameters);
        string token = await CreateUserSessionAsync(_client);

        // configuration["PortalUrl"] -> should goes to client
        var url = $"{configuration["PortalUrl"]}/?token={token}#form/{formId}/display";

        return url;
    }

    private async Task FillFormAsync(string clientId, int formId, string code, Dictionary<string, object> parameters)
    {
        dynamic requestObject = new ExpandoObject();
        var requestDictionary = (IDictionary<string, object>)requestObject;

        foreach (var kvp in parameters)
            requestDictionary[kvp.Key] = kvp.Value.ToString();

        var query = dataloadedService.GenerateSQLQuery(clientId, code);
        var data = (await dataloadedService.QueryAsync<object>(query, requestObject as object)).First();
        var fillFormResponse = await _client.PutAsync($"/forms/{formId}/save", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
        fillFormResponse.EnsureSuccessStatusCode();
    }
}

public static class IntegrationServiceExtension
{
    public static IServiceCollection RegisterIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("ReiFormsLive", client =>
        {
            client.BaseAddress = new Uri(configuration["DeveloperApiBaseUrl"].ToString());
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {configuration["DeveloperApiToken"]}");
        });
        services.AddSingleton<IntegrationService>();
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
