using FormBuilder.Models.Configs;
using FormBuilder.Models.DAOs;
using FormBuilder.Models.DTO;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Dynamic;
using System.Text;

namespace FormBuilder.Services;

public class IntegrationService(IHttpClientFactory httpClientFactory, IOptions<REIFormConfig> configs, DataAccessService dataAccessService, RemoteDataFetcher dataloadedService)
{
    public async Task<IEnumerable<UserTemplate>> GetTemplatesAsync(string clientId, string organizationId)
    {
        using var _apiClient = await GetApiClientAsync(clientId, organizationId);
        var response = await _apiClient.GetAsync("/user-templates");
        return await response.Content.ReadFromJsonAsync<IEnumerable<UserTemplate>>();
    }

    private async Task<HttpClient> GetApiClientAsync(string clientId, string organizationId)
    {
        var organization = await dataAccessService.GetOrganizationAsync(clientId, organizationId);
        var _apiClient = httpClientFactory.CreateClient("ReiFormsLive");
        _apiClient.BaseAddress = new Uri(configs.Value.GetDeveloperApiUrl(organization.State));
        var token = (await dataAccessService.GetOrganizationAsync(clientId, organizationId)).Token;

        var deploperApiToken = $"{configs.Value.DeveloperApiKey}:{token}";
        _apiClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(deploperApiToken)}");

        return _apiClient;
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

        await FillFormAsync(apiClient, clientId, organizationId, formId, code, parameters);
        string token = await CreateUserSessionAsync(apiClient);

        var state = (await dataAccessService.GetOrganizationAsync(clientId, organizationId)).State;
        var portalUrl = configs.Value.GetPortalUrl(state);
        var url = $"{portalUrl}/?token={token}#form/{formId}/display";

        return url;
    }

    private async Task FillFormAsync(HttpClient apiClient, string clientId, string organizationId, int formId, string code, Dictionary<string, object> parameters)
    {
        dynamic requestObject = new ExpandoObject();
        var requestDictionary = (IDictionary<string, object>)requestObject;

        foreach (var kvp in parameters)
            requestDictionary[kvp.Key] = kvp.Value.ToString();

        var query = await dataloadedService.GenerateSQLQueryAsync(clientId, organizationId, code);
        var data = (await dataloadedService.QueryAsync<object>(clientId, query, requestObject as object)).First();
        var fillFormResponse = await apiClient.PutAsync($"/forms/{formId}/save", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
        fillFormResponse.EnsureSuccessStatusCode();
    }
}