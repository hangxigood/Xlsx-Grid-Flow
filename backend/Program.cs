using XlsxGridFlow.Api.Configuration;
using XlsxGridFlow.Api.Middleware;
using XlsxGridFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<SessionSettings>(
    builder.Configuration.GetSection("SessionSettings"));
builder.Services.Configure<CorsSettings>(
    builder.Configuration.GetSection("CorsSettings"));

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = 
            System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });

// Add memory cache for session management
builder.Services.AddMemoryCache();

// Register application services
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<DiffService>();
builder.Services.AddScoped<FormulaService>();
builder.Services.AddScoped<PdfService>();

// Add CORS
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsSettings?.AllowedOrigins ?? new[] { "http://localhost:4200" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Xlsx-Grid-Flow API",
        Version = "v1",
        Description = "Backend API for Excel template processing and session management"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable CORS
app.UseCors("AllowFrontend");

// Configure Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Xlsx-Grid-Flow API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

