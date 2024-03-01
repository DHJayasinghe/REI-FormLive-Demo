using Azure;
using Azure.Data.Tables;
using FormBuilder.Models.Domain;
using FormBuilder.Models.DTO;
using FormBuilder.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var services = builder.Services;

services.AddSingleton(_ => new TableServiceClient(configuration.GetConnectionString("ConfigDb")).CreateSchemaIfNotExist());
services.RegisterIntegration(configuration);
services.AddSingleton<RemoteDataFetcher>();

var app = builder.Build();
app.UseHttpsRedirection();

app.MapGet("/index", () => "Welcome to Form-Builder API - v0.1.0");
app.MapPost("/clients", async (SaveClientRequest request, TableServiceClient serviceClient) =>
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
app.MapPost("/clients/{clientId}/organizations", async (string clientId, SaveOrganizationRequest request, TableServiceClient serviceClient) =>
{
    var tableClient = serviceClient.GetTableClient("Organization");
    var entity = new Organization(request.Id, clientId, request.PortalUrl, request.Token);
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
app.MapPut("/form/mappings", async ([FromHeader(Name = "X-API-Key")] string apiKey, SaveMappingRequest request, TableServiceClient serviceClient) =>
{
    var tableClient = serviceClient.GetTableClient("Mapping");
    var clientId = apiKey.Split(":")[0].Replace("-", "");
    var entities = request.Mappings.Select(entry => new MappingEntry(clientId, request.Code, entry.Source, entry.Target));

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
app.MapGet("/form/templates", async (IntegrationService service) => await service.GetTemplatesAsync());
app.MapPost("/form", async ([FromHeader(Name = "X-API-Key")] string apiKey, CreateFormRequest request, IntegrationService service) =>
{
    var clientId = apiKey.Split(":")[0].Replace("-", "");
    var url = await service.CreateFormAsync(clientId, apiKey.Split(":")[1], request.Id, request.Name, request.Code, request.Parameters);
    return Results.Ok(url);
});

app.Run();