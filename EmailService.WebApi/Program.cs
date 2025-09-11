using EmailService.WebApi.Services;
using EmailService.WebApi.Models;
using EmailService.WebApi.Infrastructure;
using CMS.WebApi.Data;
using CMS.WebApi.Services;
using TMS.WebApi.Services;
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
var dbIntegratedSecurity = Environment.GetEnvironmentVariable("DB_INTEGRATED_SECURITY") ?? "true";
var dbTrustServerCertificate = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "true";

// For local SQL Server Express, use named pipes for better reliability
string connectionString;
if (dbServer.Contains("SQLEXPRESS") && (dbServer.StartsWith("localhost") || dbServer.Contains(Environment.MachineName)))
{
    connectionString = $"Data Source=np:\\\\.\\pipe\\MSSQL$SQLEXPRESS\\sql\\query;Initial Catalog={dbDatabase};Integrated Security={dbIntegratedSecurity};Persist Security Info=False;TrustServerCertificate={dbTrustServerCertificate};Connection Timeout=30;";
    Console.WriteLine($"🔧 EmailService using named pipes connection for local SQLEXPRESS");
}
else
{
    connectionString = $"Data Source={dbServer};Initial Catalog={dbDatabase};Integrated Security={dbIntegratedSecurity};Persist Security Info=False;TrustServerCertificate={dbTrustServerCertificate};Connection Timeout=30;";
    Console.WriteLine($"🔧 EmailService using TCP/IP connection for remote server");
}

Console.WriteLine($"📧 EmailService Database: {dbDatabase}");

// Add services to the container.

// Add controllers with JSON enum handling - ONLY EmailService controllers
builder.Services.AddControllers(options =>
{
    // Apply controller filtering to hide CMS/TMS endpoints
    options.Conventions.Add(new ControllerExclusionConvention(typeof(Program).Assembly));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add custom application model provider for additional filtering
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider, EmailServiceApplicationModelProvider>();

// Configure Entity Framework for CMS (shared database)
builder.Services.AddDbContext<CmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register CMS Services (used internally by Email Service)
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICmsTemplateService, CmsTemplateService>();

// Register TMS Services (used internally by Email Service)
builder.Services.AddScoped<ITemplateService, TMS.WebApi.Services.TemplateService>();
builder.Services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
builder.Services.AddScoped<IDocumentEmbeddingService, DocumentEmbeddingService>();

// Register Email Service integration services
builder.Services.AddScoped<ICmsIntegrationService, CmsIntegrationService>();
builder.Services.AddScoped<ITmsIntegrationService, TmsIntegrationService>();

// Register Email Service main service
builder.Services.AddScoped<IEmailSendingService, EmailSendingService>();

// Configure Swagger/OpenAPI for Email Service endpoints ONLY
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Email Service API",
        Version = "v1",
        Description = "A powerful Email Service that integrates with TMS (Template Management System) and CMS (Content Management System) for advanced email automation with template-based content generation and document attachments. This API exposes ONLY Email Service endpoints - TMS and CMS services are used internally.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Saleh Shalab",
            Email = "salehshalab2@gmail.com"
        }
    });

    // Include XML comments for better Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Filter out non-EmailService controllers from Swagger documentation
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var controllerType = apiDesc.ActionDescriptor.RouteValues["controller"];
        return controllerType?.StartsWith("Email") == true;
    });
});

// Configure CORS if needed
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Email Service API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        await context.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error initializing database");
    }
}

// Log startup information
app.Logger.LogInformation("🚀 Email Service API is starting...");
app.Logger.LogInformation("📋 Available endpoints:");
app.Logger.LogInformation("     POST /api/email/send-with-template - Send email with TMS template");
app.Logger.LogInformation("     POST /api/email/send-with-documents - Send email with CMS documents");
app.Logger.LogInformation("     GET  /api/email/accounts - Get available email accounts");
app.Logger.LogInformation("     GET  /api/email/health - Health check");
app.Logger.LogInformation("🔧 Swagger UI available at: /");

app.Run();
