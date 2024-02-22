using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sample.Pages;

public class IndexModel(IHttpClientFactory clientFactory) : PageModel
{
    private readonly HttpClient _client = clientFactory.CreateClient("ReiFormsLive");
    public IEnumerable<UserTemplate> Templates { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var response = await _client.GetAsync("/user-templates");
        Templates = await response.Content.ReadFromJsonAsync<IEnumerable<UserTemplate>>();
    }
}

public record UserTemplate(int Id, string Name, int Template_Id, string Template_Name, string Template_Code);