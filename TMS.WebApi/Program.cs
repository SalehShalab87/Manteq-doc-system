using CMS.WebApi.Data;
using CMS.WebApi.Services;
using TMS.WebApi.Services;
using TMS.WebApi.Infrastructure;
using TMS.WebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

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

// Build database connection string from environment variables
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "YOUR_SERVER\\SQLEXPRESS";
var dbDatabase = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "CmsDatabase_Dev";
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var dbIntegratedSecurity = Environment.GetEnvironmentVariable("DB_INTEGRATED_SECURITY") ?? "true";
var dbTrustServerCertificate = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "true";

// For local SQL Server Express, use named pipes for better reliability
string connectionString;
if (dbServer.Contains("SQLEXPRESS") && (dbServer.StartsWith("localhost") || dbServer.Contains(Environment.MachineName)))
{
    connectionString = $"Data Source=np:\\\\.\\pipe\\MSSQL$SQLEXPRESS\\sql\\query;Initial Catalog={dbDatabase};Integrated Security={dbIntegratedSecurity};Persist Security Info=False;TrustServerCertificate={dbTrustServerCertificate};Connection Timeout=30;";
    Console.WriteLine($"🔧 TMS using named pipes connection for local SQLEXPRESS");
}
else
{
    // Check if we should use SQL Authentication or Windows Authentication
    if (dbIntegratedSecurity.ToLower() == "false" && !string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPassword))
    {
        connectionString = $"Data Source={dbServer};Initial Catalog={dbDatabase};User ID={dbUser};Password={dbPassword};Persist Security Info=False;TrustServerCertificate={dbTrustServerCertificate};Connection Timeout=30;";
        Console.WriteLine($"🔧 TMS using SQL Authentication for server: {dbServer}");
    }
    else
    {
        connectionString = $"Data Source={dbServer};Initial Catalog={dbDatabase};Integrated Security={dbIntegratedSecurity};Persist Security Info=False;TrustServerCertificate={dbTrustServerCertificate};Connection Timeout=30;";
        Console.WriteLine($"🔧 TMS using Integrated Security for server: {dbServer}");
    }
}

Console.WriteLine($"📄 TMS Database: {dbDatabase}");

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

// Configure Entity Framework for CMS
builder.Services.AddDbContext<CmsDbContext>(options =>
{
    // Use connection string built from environment variables
    options.UseSqlServer(connectionString);
});

// Add HttpContextAccessor (required by DocumentService for X-SME-UserId header)
builder.Services.AddHttpContextAccessor();

// Register CMS Services (used internally by TMS - no CMS endpoints are exposed)
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICmsTemplateService, CmsTemplateService>();

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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
    try
    {
        context.Database.EnsureCreated();
        app.Logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error initializing database");
    }
}

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
