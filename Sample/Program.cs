var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Add services to the container.
var services = builder.Services;
services.AddRazorPages();
services.AddHttpClient("ReiFormsLive", client =>
{
    client.BaseAddress = new Uri(configuration["DeveloperApiBaseUrl"].ToString());
    client.DefaultRequestHeaders.Add("Authorization", $"Basic {configuration["DeveloperApiToken"]}");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
