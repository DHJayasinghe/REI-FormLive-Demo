namespace FormBuilder.Models.Configs;

public class REIFormConfig
{
    public string Environment { get; set; }
    public string PortalUrlFormat { get; set; }
    public string DeveloperApiUrlFormat { get; set; }
    public string DeveloperApiKey { get; set; }

    public string GetPortalUrl(string state) => string.Format("https://{0}.{1}.formslive.com.au", state, Environment);
    public string GetDeveloperApiUrl(string state) => string.Format("https://{0}-api.{1}.formslive.com.au", state, Environment);
}
