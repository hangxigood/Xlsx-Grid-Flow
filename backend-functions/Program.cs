using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;
using XlsxGridFlow.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Add Application Insights telemetry
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Register Azure Blob Storage
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (!string.IsNullOrEmpty(storageConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(storageConnectionString));
        }

        // Register application services
        services.AddSingleton<ExcelService>();
        services.AddSingleton<BlobSessionService>();
        services.AddSingleton<DiffService>();
        services.AddSingleton<FormulaService>();
        services.AddSingleton<PdfService>();
    })
    .Build();

await host.RunAsync();
