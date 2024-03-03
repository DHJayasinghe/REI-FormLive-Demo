using Azure;
using Azure.Data.Tables;
using FormBuilder.Models.DAOs;
using FormBuilder.Models.DTO;
using FormBuilder.Services;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var services = builder.Services;

services.RegisterIntegration(configuration);

var app = builder.Build();
app.UseHttpsRedirection();

app.MapGet("/index", () => "Welcome to Form-Builder API - v0.1.0");

#region Administrative APIs
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
app.MapPut("/clients/{clientId}/organizations", async (string clientId, SaveOrganizationRequest request, TableServiceClient serviceClient) =>
{
    var tableClient = serviceClient.GetTableClient("Organization");
    var entity = new Organization(request.Id, clientId, request.State, request.Token);
    try
    {
        await tableClient.UpsertEntityAsync(entity);
        return Results.Ok();
    }
    catch (RequestFailedException ex)
    {
        return Results.Problem(ex.Message);
    }
});
#endregion

#region Client APIs
app.MapPut("/form/mappings", async (Identity identity, SaveMappingRequest request, TableServiceClient serviceClient) =>
{
    var tableClient = serviceClient.GetTableClient("Mapping");
    var entities = request.Mappings.Select(entry => new MappingEntry(identity.ClientId, identity.OrganizationId, request.Code, entry.Source, entry.Target));

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
app.MapGet("/form/templates", async (Identity identity, IntegrationService service) => service.GetTemplatesAsync(identity.ClientId, identity.OrganizationId));
app.MapPost("/form", async (Identity identity, CreateFormRequest request, IntegrationService service) =>
{
    var url = await service.CreateFormAsync(identity.ClientId, identity.OrganizationId, request.Id, request.Name, request.Code, request.Parameters);
    return Results.Ok(url);
});
#endregion

app.Run();