using TMS.WebApi.Services;
using TMS.WebApi.Infrastructure;
using TMS.WebApi.Models;
using TMS.WebApi.HttpClients;
using System.Text.Json.Serialization;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file if exists
if (File.Exists(".env"))
{
    foreach (var line in File.ReadAllLines(".env"))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

// Get CMS API configuration
var cmsApiBaseUrl = Environment.GetEnvironmentVariable("CMS_BASE_URL") 
    ?? builder.Configuration["CmsApi:BaseUrl"] 
    ?? "http://localhost:5000";

var cmsApiTimeout = int.TryParse(
    Environment.GetEnvironmentVariable("CMS_API_TIMEOUT") ?? builder.Configuration["CmsApi:Timeout"],
    out var timeout) ? timeout : 30;

Console.WriteLine($"🔗 TMS will connect to CMS API: {cmsApiBaseUrl}");
Console.WriteLine($"⏱️  CMS API timeout: {cmsApiTimeout} seconds");

// Configure TMS Settings
builder.Services.Configure<TmsSettings>(builder.Configuration.GetSection("TMS"));

// Add services to the container.

// Configure JSON serialization to handle enums properly
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add controllers - only from TMS.WebApi assembly (excludes CMS controllers)
builder.Services.AddControllers(options =>
{
    // Add a custom filter to exclude CMS controllers
    options.Conventions.Add(new ControllerExclusionConvention());
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Configure HTTP Client for CMS API with Polly resilience policies
builder.Services.AddHttpClient<ICmsApiClient, CmsApiClient>(client =>
{
    client.BaseAddress = new Uri(cmsApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(cmsApiTimeout);
    client.DefaultRequestHeaders.Add("User-Agent", "TMS-API/1.0");
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"⚠️ TMS → CMS API retry {retryCount} after {timespan.TotalSeconds}s");
        }))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Console.WriteLine($"❌ TMS → CMS API circuit breaker opened for {duration.TotalSeconds}s");
        },
        onReset: () =>
        {
            Console.WriteLine($"✅ TMS → CMS API circuit breaker reset");
        }));

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register TMS Services (these power the exposed TMS API endpoints)
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
builder.Services.AddScoped<IDocumentEmbeddingService, DocumentEmbeddingService>();
builder.Services.AddScoped<IExcelService, ExcelService>();

// Configure Swagger/OpenAPI - only for TMS endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Template Management System (TMS) API",
        Version = "v1",
        Description = "A powerful Template Management System that uses CMS for data storage and provides document generation with multiple export formats. This API exposes only TMS endpoints - CMS services are used internally.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Saleh Manteq",
            Email = "saleh@manteq.com"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

});

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201",
                "https://localhost:4200",
                "https://localhost:4201")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TMS API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "TMS API",
    version = "v1",
    timestamp = DateTime.UtcNow
}));

// Root endpoint for production
app.MapGet("/", () => Results.Ok(new
{
    service = "Template Management System (TMS) API",
    version = "v1",
    status = "running",
    endpoints = new[]
    {
        "GET /health - Health check",
        "POST /api/templates/register - Register template",
        "GET /api/templates/{id} - Get template by ID",
        "GET /api/templates/{id}/properties - Get template properties",
        "GET /api/templates/template-types - Get template types",
        "GET /api/templates/export-formats - Get export formats",
        "GET /api/templates/{id}/analytics - Get template analytics",
        "GET /api/templates/{id}/download-placeholders-excel - Download placeholders Excel",
        "POST /api/templates/{id}/test-generate - Test document generation",
        "POST /api/templates/generate - Generate document",
        "POST /api/templates/generate-with-embeddings - Generate with embeddings",
        "GET /api/templates/download/{id} - Download generated document"
    },
    swagger = app.Environment.IsDevelopment() ? "/swagger" : null
}));

app.MapControllers();

app.Logger.LogInformation("🚀 Template Management System (TMS) API is starting...");
app.Logger.LogInformation("📋 Available endpoints:");
app.Logger.LogInformation("   POST /api/templates/register - Register new template");
app.Logger.LogInformation("   GET  /api/templates/{{id}} - Retrieve template");
app.Logger.LogInformation("   GET  /api/templates/{{id}}/properties - Get template properties");
app.Logger.LogInformation("   GET  /api/templates/{{id}}/download-placeholders-excel - Download placeholders as Excel");
app.Logger.LogInformation("   POST /api/templates/{{id}}/test-generate - Test generate with Excel upload");
app.Logger.LogInformation("   POST /api/templates/generate - Generate document from template");
app.Logger.LogInformation("   POST /api/templates/generate-with-embeddings - Generate document with embeddings");
app.Logger.LogInformation("   GET  /api/templates/download/{{id}} - Download generated document");
app.Logger.LogInformation("🔧 Swagger UI available at: /");

app.Run();
