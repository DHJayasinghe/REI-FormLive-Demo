using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Sample.Services;
using System.Text;

namespace Sample.Pages;

[BindProperties]
public class CreateFormModel(IHttpClientFactory clientFactory, IConfiguration configuration, DataLoaderService dataloadedService) : PageModel
{
    private readonly HttpClient _client = clientFactory.CreateClient("ReiFormsLive");

    public int Id { get; set; }
    public string? Name { get; set; }

    public void OnGet(int id)
    {
        Id = id;
    }

    public async Task OnPostAsync()
    {
        FormPostResponse createFormResult = await CreateFormAsync();
        var formId = createFormResult.Id;
        await FillFormAsync(formId);
        string token = await CreateUserSessionAsync();
        TempData["DeepLink"] = $"{configuration["PortalUrl"]}/?token={token}#form/{formId}/display";
    }

    private async Task<string> CreateUserSessionAsync()
    {
        var createSessionResponse = await _client.PostAsync("/user/session", new StringContent("", Encoding.UTF8, "application/json"));
        createSessionResponse.EnsureSuccessStatusCode();

        var createSessionResult = await createSessionResponse.Content.ReadFromJsonAsync<SessionPostResponse>();
        var token = createSessionResult.Token;
        return token;
    }

    private async Task<FormPostResponse?> CreateFormAsync()
    {
        var createFormResponse = await _client.PostAsync($"/user-templates/{Id}/form", new StringContent(JsonConvert.SerializeObject(new
        {
            name = Name,
            isPrivate = false
        }), Encoding.UTF8, "application/json"));
        createFormResponse.EnsureSuccessStatusCode();
        var createFormResult = await createFormResponse.Content.ReadFromJsonAsync<FormPostResponse>();
        return createFormResult;
    }

    private async Task FillFormAsync(int formId)
    {
        var query = dataloadedService.GenerateSQLQuery("[Office].OfficeId=@OfficeID");
        var data = await dataloadedService.QueryAsync<FormLiveData>(query, new { OfficeID = 50 });
        var fillFormResponse = await _client.PutAsync($"/forms/{formId}/save", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
        fillFormResponse.EnsureSuccessStatusCode();
    }
}

public record FormPostResponse(int Id);
public record SessionPostResponse(string Token);
