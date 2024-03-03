using Azure.Data.Tables;
using FormBuilder.Models.Configs;

namespace FormBuilder.Services;

public static class IntegrationServiceRegistation
{
    public static IServiceCollection RegisterIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<REIFormConfig>(configuration.GetSection(nameof(REIFormConfig)));
        services.AddSingleton(_ => new TableServiceClient(configuration.GetConnectionString("ConfigDb")).CreateSchemaIfNotExist());
        services.AddHttpContextAccessor();
        services.AddHttpClient("ReiFormsLive");
        services.AddSingleton<DataAccessService>();
        services.AddSingleton<IntegrationService>();
        services.AddSingleton<RemoteDataFetcher>();
        services.AddScoped<Identity>();
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
