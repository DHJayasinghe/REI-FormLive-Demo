using Azure;
using Azure.Data.Tables;
using FormBuilder.Models.Domain;
using FormBuilder.Models.DTO;
using FormBuilder.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var services = builder.Services;

var tableServiceClient = new TableServiceClient(configuration.GetConnectionString("ConfigDb"));
services.AddScoped(_ => tableServiceClient);
services.AddHttpClient("ReiFormsLive", client =>
    {
        client.BaseAddress = new Uri(configuration["DeveloperApiBaseUrl"].ToString());
        client.DefaultRequestHeaders.Add("Authorization", $"Basic {configuration["DeveloperApiToken"]}");
    });
services.AddSingleton<RemoteDataFetcher>();

tableServiceClient.GetTableClient("Client").CreateIfNotExists();
tableServiceClient.GetTableClient("Mapping").CreateIfNotExists();


var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/index", () => "Welcome to Form-Builder API - v0.1.0");
app.MapPost("/client", async (
    SaveClientRequest request,
    TableServiceClient serviceClient) =>
{
    var tableClient = serviceClient.GetTableClient("Client");
    var entity = new Client(request.Name, request.ConnectionString);
    try
    {
        await tableClient.AddEntityAsync(entity);
        return Results.Created();
    }
    catch (RequestFailedException ex)
    {
        return Results.Problem(ex.Message);
    }
});
app.MapPut("/mapping/{code}", async (
    string code,
    [FromHeader(Name = "X-API-Key")] string apiKey,
    SaveMappingRequest request,
    TableServiceClient serviceClient) =>
{
    var tableClient = serviceClient.GetTableClient("Mapping");
    var clientId = apiKey.Replace("-", "");
    var entities = request.Mappings.Select(entry => new MappingEntry(clientId, code, entry.Source, entry.Target));

    try
    {
        var addTasks = entities.Select(entity => tableClient.UpsertEntityAsync(entity));
        await Task.WhenAll(addTasks);
        return Results.Ok();
    }
    catch (RequestFailedException ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();