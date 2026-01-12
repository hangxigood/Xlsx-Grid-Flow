# Native AOT + Azure Functions Migration Plan
## Xlsx-Grid-Flow Serverless Architecture

---

## 🎯 **Migration Goals (.NET 10 LTS with Native AOT)**

- ✅ Reduce cold start from 2-3s to **5-40ms** (98% improvement) ⚡
- ✅ Reduce memory usage from 150MB to **15-30MB** (85% reduction) 💾
- ✅ Reduce deployment size from 80MB to **3-10MB** (90% reduction) 📦
- ✅ Reduce monthly costs from $13-70 to **$0-2** (97% savings) 💰
- ✅ Enable true serverless auto-scaling with LTS support until 2028 🚀

---

## 📋 **Pre-Migration Checklist**

### **Environment Setup**
- [ ] Install .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
- [ ] Install Azure Functions Core Tools v4: `npm install -g azure-functions-core-tools@4`
- [ ] Install Azure CLI: `brew install azure-cli` (macOS)
- [ ] Login to Azure: `az login`
- [ ] Verify Docker is installed (for local testing)

### **Azure Resources**
- [ ] Create Azure Storage Account (for session state)
- [ ] Create Azure Function App (Consumption or Flex Consumption plan)
- [ ] Configure CORS for GitHub Pages origin
- [ ] Set up Application Insights (optional but recommended)

---

## 🔧 **Phase 1: Convert to Azure Functions (Standard .NET)**

### **Step 1.1: Create New Function App Project**

```bash
# Navigate to your project root
cd /Users/xianghangxi/Github/Xlsx-Grid-Flow

# Create new Functions project
mkdir backend-functions
cd backend-functions

# Initialize Functions project
func init . --worker-runtime dotnet-isolated --target-framework net10.0

# Add required packages
dotnet add package Microsoft.Azure.Functions.Worker --version 2.0.0
dotnet add package Microsoft.Azure.Functions.Worker.Sdk --version 2.0.0
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http --version 3.2.0
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Timer --version 4.3.1
dotnet add package Azure.Storage.Blobs --version 12.22.0
dotnet add package EPPlus --version 7.5.2
dotnet add package QuestPDF --version 2024.12.3
```

### **Step 1.2: Migrate Services**

Copy your existing services with minimal changes:

```
backend/Services/
├── ExcelService.cs      → backend-functions/Services/ExcelService.cs
├── SessionService.cs    → backend-functions/Services/BlobSessionService.cs (modified)
├── DiffService.cs       → backend-functions/Services/DiffService.cs
└── PdfService.cs        → backend-functions/Services/PdfService.cs
```

### **Step 1.3: Create HTTP Trigger Functions**

#### **Function 1: Upload Template**

```csharp
// Functions/UploadTemplateFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace XlsxGridFlow.Functions;

public class UploadTemplateFunction
{
    private readonly ILogger<UploadTemplateFunction> _logger;
    private readonly ExcelService _excelService;
    private readonly BlobSessionService _sessionService;

    public UploadTemplateFunction(
        ILogger<UploadTemplateFunction> logger,
        ExcelService excelService,
        BlobSessionService sessionService)
    {
        _logger = logger;
        _excelService = excelService;
        _sessionService = sessionService;
    }

    [Function("UploadTemplate")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "template/upload")] 
        HttpRequestData req)
    {
        _logger.LogInformation("Processing template upload");

        try
        {
            // Parse multipart form data
            var formData = await req.ReadFormDataAsync();
            var file = formData.Files["file"];
            
            if (file == null)
            {
                return await CreateErrorResponse(req, "No file uploaded", HttpStatusCode.BadRequest);
            }

            // Parse Excel file
            using var stream = file.OpenReadStream();
            var template = await _excelService.ParseExcelAsync(stream, file.FileName);

            // Create session
            var sessionId = Guid.NewGuid().ToString();
            await _sessionService.CreateSessionAsync(sessionId, template);

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                sessionId,
                template
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading template");
            return await CreateErrorResponse(req, ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, 
        string message, 
        HttpStatusCode statusCode)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}
```

#### **Function 2: Save Session**

```csharp
// Functions/SaveSessionFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace XlsxGridFlow.Functions;

public class SaveSessionFunction
{
    private readonly ILogger<SaveSessionFunction> _logger;
    private readonly BlobSessionService _sessionService;
    private readonly DiffService _diffService;
    private readonly FormulaService _formulaService;

    public SaveSessionFunction(
        ILogger<SaveSessionFunction> logger,
        BlobSessionService sessionService,
        DiffService diffService,
        FormulaService formulaService)
    {
        _logger = logger;
        _sessionService = sessionService;
        _diffService = diffService;
        _formulaService = formulaService;
    }

    [Function("SaveSession")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/save")] 
        HttpRequestData req,
        string sessionId)
    {
        _logger.LogInformation($"Saving session: {sessionId}");

        try
        {
            // Get current session
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return await CreateErrorResponse(req, "Session not found", HttpStatusCode.NotFound);
            }

            // Parse new data from request
            var newData = await req.ReadFromJsonAsync<SaveRequest>();
            if (newData?.RowData == null)
            {
                return await CreateErrorResponse(req, "Invalid request", HttpStatusCode.BadRequest);
            }

            // Recalculate formulas server-side
            var recalculatedData = await _formulaService.RecalculateFormulasAsync(
                session.Template.ColumnDefs,
                newData.RowData
            );

            // Calculate diff
            var changes = _diffService.CalculateDiff(
                session.CurrentSnapshot,
                recalculatedData
            );

            // Update session
            session.Version++;
            session.CurrentSnapshot = recalculatedData;
            session.ChangeLog.AddRange(changes.Select(c => new AuditLogEntry
            {
                Version = session.Version,
                Timestamp = DateTime.UtcNow,
                CellReference = c.CellReference,
                OldValue = c.OldValue,
                NewValue = c.NewValue
            }));

            await _sessionService.UpdateSessionAsync(sessionId, session);

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                version = session.Version,
                changes = changes.Count
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving session {sessionId}");
            return await CreateErrorResponse(req, ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, 
        string message, 
        HttpStatusCode statusCode)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}

public record SaveRequest(List<GridRow> RowData);
```

#### **Function 3: Export PDF**

```csharp
// Functions/ExportPdfFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace XlsxGridFlow.Functions;

public class ExportPdfFunction
{
    private readonly ILogger<ExportPdfFunction> _logger;
    private readonly BlobSessionService _sessionService;
    private readonly PdfService _pdfService;

    public ExportPdfFunction(
        ILogger<ExportPdfFunction> logger,
        BlobSessionService sessionService,
        PdfService pdfService)
    {
        _logger = logger;
        _sessionService = sessionService;
        _pdfService = pdfService;
    }

    [Function("ExportPdf")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/export/pdf")] 
        HttpRequestData req,
        string sessionId)
    {
        _logger.LogInformation($"Exporting PDF for session: {sessionId}");

        try
        {
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return await CreateErrorResponse(req, "Session not found", HttpStatusCode.NotFound);
            }

            // Generate PDF
            var pdfBytes = await _pdfService.GeneratePdfAsync(
                session.Template,
                session.CurrentSnapshot,
                session.ChangeLog
            );

            // Return PDF
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");
            response.Headers.Add("Content-Disposition", 
                $"attachment; filename=\"{session.Template.Filename}.pdf\"");
            await response.Body.WriteAsync(pdfBytes);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error exporting PDF for session {sessionId}");
            return await CreateErrorResponse(req, ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, 
        string message, 
        HttpStatusCode statusCode)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}
```

#### **Function 4: Cleanup Timer**

```csharp
// Functions/CleanupExpiredSessionsFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace XlsxGridFlow.Functions;

public class CleanupExpiredSessionsFunction
{
    private readonly ILogger<CleanupExpiredSessionsFunction> _logger;
    private readonly BlobSessionService _sessionService;

    public CleanupExpiredSessionsFunction(
        ILogger<CleanupExpiredSessionsFunction> logger,
        BlobSessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    [Function("CleanupExpiredSessions")]
    public async Task Run(
        [TimerTrigger("0 */30 * * * *")] TimerInfo timer) // Every 30 minutes
    {
        _logger.LogInformation("Starting session cleanup");

        try
        {
            var deletedCount = await _sessionService.CleanupExpiredSessionsAsync();
            _logger.LogInformation($"Cleaned up {deletedCount} expired sessions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup");
        }
    }
}
```

### **Step 1.4: Create Blob Session Service**

```csharp
// Services/BlobSessionService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Json;

namespace XlsxGridFlow.Functions.Services;

public class BlobSessionService
{
    private readonly BlobContainerClient _containerClient;
    private const int SessionExpirationMinutes = 30;

    public BlobSessionService(BlobServiceClient blobServiceClient)
    {
        _containerClient = blobServiceClient.GetBlobContainerClient("sessions");
        _containerClient.CreateIfNotExists();
    }

    public async Task CreateSessionAsync(string sessionId, Template template)
    {
        var session = new SessionState
        {
            SessionId = sessionId,
            Version = 0,
            Template = template,
            CurrentSnapshot = template.RowData,
            ChangeLog = new List<AuditLogEntry>()
        };

        await SaveSessionAsync(sessionId, session);
    }

    public async Task<SessionState?> GetSessionAsync(string sessionId)
    {
        var blobClient = _containerClient.GetBlobClient($"{sessionId}.json");
        
        if (!await blobClient.ExistsAsync())
            return null;

        var download = await blobClient.DownloadContentAsync();
        return JsonSerializer.Deserialize<SessionState>(download.Value.Content.ToString());
    }

    public async Task UpdateSessionAsync(string sessionId, SessionState session)
    {
        await SaveSessionAsync(sessionId, session);
    }

    private async Task SaveSessionAsync(string sessionId, SessionState session)
    {
        var blobClient = _containerClient.GetBlobClient($"{sessionId}.json");
        var json = JsonSerializer.Serialize(session);
        
        await blobClient.UploadAsync(
            BinaryData.FromString(json),
            overwrite: true
        );

        // Set metadata for expiration
        var metadata = new Dictionary<string, string>
        {
            ["ExpiresAt"] = DateTime.UtcNow.AddMinutes(SessionExpirationMinutes).ToString("o"),
            ["CreatedAt"] = DateTime.UtcNow.ToString("o")
        };
        await blobClient.SetMetadataAsync(metadata);
    }

    public async Task<int> CleanupExpiredSessionsAsync()
    {
        var deletedCount = 0;
        var now = DateTime.UtcNow;

        await foreach (var blob in _containerClient.GetBlobsAsync(BlobTraits.Metadata))
        {
            if (blob.Metadata.TryGetValue("ExpiresAt", out var expiresAtStr))
            {
                if (DateTime.Parse(expiresAtStr) < now)
                {
                    await _containerClient.DeleteBlobAsync(blob.Name);
                    deletedCount++;
                }
            }
        }

        return deletedCount;
    }
}
```

### **Step 1.5: Configure Dependency Injection**

```csharp
// Program.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;
using XlsxGridFlow.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Register Azure Blob Storage
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        services.AddSingleton(new BlobServiceClient(storageConnectionString));

        // Register services
        services.AddSingleton<ExcelService>();
        services.AddSingleton<BlobSessionService>();
        services.AddSingleton<DiffService>();
        services.AddSingleton<FormulaService>();
        services.AddSingleton<PdfService>();
    })
    .Build();

await host.RunAsync();
```

### **Step 1.6: Configure local.settings.json**

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ASPNETCORE_ENVIRONMENT": "Development"
  },
  "Host": {
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

---

## 🚀 **Phase 2: Enable Native AOT**

### **Step 2.1: Update .csproj for Native AOT**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AzureFunctionsVersion>v4</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Enable Native AOT -->
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
    <StripSymbols>true</StripSymbols>
    <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
    
    <!-- Trim unused code -->
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>full</TrimMode>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.0.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="2.0.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.2.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Timer" Version="4.3.1" />
    <PackageReference Include="Azure.Storage.Blobs" Version="12.22.0" />
    
    <!-- Test EPPlus/QuestPDF compatibility - may need alternatives -->
    <PackageReference Include="EPPlus" Version="7.5.2" />
    <PackageReference Include="QuestPDF" Version="2024.12.3" />
  </ItemGroup>

</Project>
```

### **Step 2.2: Add JSON Source Generators (Required for AOT)**

```csharp
// Models/JsonContext.cs
using System.Text.Json.Serialization;

namespace XlsxGridFlow.Functions.Models;

[JsonSerializable(typeof(Template))]
[JsonSerializable(typeof(SessionState))]
[JsonSerializable(typeof(AuditLogEntry))]
[JsonSerializable(typeof(GridRow))]
[JsonSerializable(typeof(ColumnDef))]
[JsonSerializable(typeof(SaveRequest))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppJsonContext : JsonSerializerContext
{
}
```

Update serialization calls:

```csharp
// Before:
var json = JsonSerializer.Serialize(session);

// After (AOT-compatible):
var json = JsonSerializer.Serialize(session, AppJsonContext.Default.SessionState);
```

### **Step 2.3: Test AOT Compatibility**

```bash
# Analyze AOT warnings
dotnet publish -c Release /p:PublishAot=true

# Check for warnings like:
# - IL2026: Reflection usage
# - IL2087: Unrecognized pattern
# - IL3050: Dynamic code generation
```

### **Step 2.4: Handle EPPlus/QuestPDF Compatibility**

**Option A: Test Current Libraries**
```bash
dotnet publish -c Release /p:PublishAot=true
# If it works, great! If not, see Option B
```

**Option B: Replace with AOT-Compatible Alternatives**

If EPPlus/QuestPDF don't support AOT:

```csharp
// For Excel: Use ClosedXML or NPOI (check AOT support)
// For PDF: Use iText7 or PdfSharp (check AOT support)

// Or: Create a separate non-AOT function just for PDF generation
// (Most functions are AOT, PDF function uses standard .NET)
```

---

## 📦 **Phase 3: Deploy to Azure**

### **Step 3.1: Create Azure Resources**

```bash
# Variables
RESOURCE_GROUP="xlsx-grid-flow-rg"
LOCATION="eastus"
STORAGE_ACCOUNT="xlsxgridflowstorage"
FUNCTION_APP="xlsx-grid-flow-functions"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Create Function App (Consumption plan with .NET 9)
az functionapp create \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_ACCOUNT \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 10 \
  --functions-version 4 \
  --os-type Linux

# Configure CORS
az functionapp cors add \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --allowed-origins "https://hangxigood.github.io"
```

### **Step 3.2: Publish Native AOT Function**

```bash
# Build and publish
cd backend-functions
dotnet publish -c Release /p:PublishAot=true

# Deploy to Azure
func azure functionapp publish $FUNCTION_APP --dotnet-isolated
```

### **Step 3.3: Verify Deployment**

```bash
# Test upload endpoint
curl -X POST https://xlsx-grid-flow-functions.azurewebsites.net/api/template/upload \
  -F "file=@testbook.xlsx"

# Check cold start time
time curl https://xlsx-grid-flow-functions.azurewebsites.net/api/health
```

---

## 🧪 **Testing Checklist**

- [ ] Local testing with Azurite (Azure Storage Emulator)
- [ ] Upload Excel file and verify parsing
- [ ] Save session and verify diff calculation
- [ ] Export PDF and verify generation
- [ ] Test session expiration and cleanup
- [ ] Verify cold start time (<500ms)
- [ ] Verify memory usage (<50MB)
- [ ] Load test with multiple concurrent requests
- [ ] Test CORS from GitHub Pages frontend

---

## 📊 **Monitoring & Optimization**

### **Application Insights Queries**

```kusto
// Cold start times
requests
| where cloud_RoleName == "xlsx-grid-flow-functions"
| where name startswith "UploadTemplate"
| summarize avg(duration), max(duration), min(duration) by bin(timestamp, 1h)

// Memory usage
performanceCounters
| where name == "Private Bytes"
| summarize avg(value) by bin(timestamp, 5m)
```

---

## 🎯 **Success Metrics**

| Metric | Target | How to Measure |
|--------|--------|----------------|
| Cold Start | <500ms | Application Insights |
| Memory Usage | <50MB | Azure Portal Metrics |
| Deployment Size | <25MB | Check published folder |
| Monthly Cost | <$5 | Azure Cost Management |
| Response Time | <200ms | Application Insights |

---

## 🚨 **Rollback Plan**

If Native AOT causes issues:

1. Remove `<PublishAot>true</PublishAot>` from .csproj
2. Redeploy with standard .NET runtime
3. Still get 90% of serverless benefits (just slower cold starts)

---

## 📚 **Additional Resources**

- [.NET 9 Native AOT Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Azure Functions .NET Isolated Worker](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [Native AOT Compatibility](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/compatibility)

---

**Next Steps:**
1. Review this plan
2. Set up Azure resources
3. Start with Phase 1 (standard Functions)
4. Test thoroughly
5. Enable Native AOT (Phase 2)
6. Deploy and monitor

Good luck! 🚀
