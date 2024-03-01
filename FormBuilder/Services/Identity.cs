namespace FormBuilder.Services;

public class Identity(IHttpContextAccessor httpContextAccessor)
{
    private HttpContext _context = httpContextAccessor.HttpContext;
    private string ApiKey => _context.Request.Headers["X-API-Key"];
    private string[] ApiKeyParts => !string.IsNullOrEmpty(ApiKey) ? ApiKey.Split(':') : [];
    public string ClientId => ApiKeyParts.Length > 0 ? ApiKeyParts[0] : string.Empty;
    public string OrganizationId => ApiKeyParts.Length == 2 ? ApiKeyParts[1] : string.Empty;
}
