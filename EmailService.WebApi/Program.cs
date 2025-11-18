using EmailService.WebApi.Services;
using EmailService.WebApi.Models;
using EmailService.WebApi.Infrastructure;
using EmailService.WebApi.HttpClients;
using System.Text.Json.Serialization;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// ⚠️ EmailService is now stateless - NO database access
// All data operations go through HTTP APIs (CMS and TMS)
Console.WriteLine($"📧 EmailService (Stateless) - Using HTTP APIs for CMS and TMS");

// Add services to the container.

// Add controllers - ONLY EmailService controllers
builder.Services.AddControllers(options =>
{
    // Apply controller filtering to hide CMS/TMS endpoints
    options.Conventions.Add(new ControllerExclusionConvention(typeof(Program).Assembly));
});

// Add custom application model provider for additional filtering
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider, EmailServiceApplicationModelProvider>();

// ⚠️ NO Entity Framework - EmailService is stateless

// Add HttpContextAccessor for header reading
builder.Services.AddHttpContextAccessor();

// Get API configuration from environment variables or appsettings
var cmsApiBaseUrl = Environment.GetEnvironmentVariable("CMS_BASE_URL") 
                   ?? builder.Configuration["CmsApi:BaseUrl"] 
                   ?? "http://localhost:5000";
var cmsApiTimeout = int.Parse(builder.Configuration["CmsApi:Timeout"] ?? "30");

var tmsApiBaseUrl = Environment.GetEnvironmentVariable("TMS_BASE_URL") 
                   ?? builder.Configuration["TmsApi:BaseUrl"] 
                   ?? "http://localhost:5267";
var tmsApiTimeout = int.Parse(builder.Configuration["TmsApi:Timeout"] ?? "60");

Console.WriteLine($"🔗 CMS API: {cmsApiBaseUrl} (Timeout: {cmsApiTimeout}s)");
Console.WriteLine($"🔗 TMS API: {tmsApiBaseUrl} (Timeout: {tmsApiTimeout}s)");

// Polly retry policy: 3 retries with exponential backoff
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"⚠️ Retry {retryCount} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        });

// Polly circuit breaker: Open circuit after 5 consecutive failures, keep open for 30 seconds
var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Console.WriteLine($"🔴 Circuit breaker opened for {duration.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        },
        onReset: () =>
        {
            Console.WriteLine($"🟢 Circuit breaker reset");
        });

// Register typed HTTP client for CMS API with Polly resilience
builder.Services.AddHttpClient<ICmsApiClient, CmsApiClient>(client =>
{
    client.BaseAddress = new Uri(cmsApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(cmsApiTimeout);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// Register named HttpClient for EmailTemplateIntegrationService
builder.Services.AddHttpClient("CmsApi", client =>
{
    client.BaseAddress = new Uri(cmsApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(cmsApiTimeout);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// Register typed HTTP client for TMS API with Polly resilience
builder.Services.AddHttpClient<ITmsApiClient, TmsApiClient>(client =>
{
    client.BaseAddress = new Uri(tmsApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(tmsApiTimeout);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// ⚠️ Removed all CMS/TMS service registrations - using HTTP APIs instead

// Register Email Service integration services (they will use HTTP clients)
builder.Services.AddScoped<ICmsIntegrationService, CmsIntegrationService>();
builder.Services.AddScoped<ITmsIntegrationService, TmsIntegrationService>();
builder.Services.AddScoped<EmailService.WebApi.Services.IEmailTemplateService, EmailTemplateIntegrationService>();

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

    // Use fully qualified names to avoid enum conflicts between CMS and EmailService
    c.CustomSchemaIds(type => type.FullName);


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

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Email Service API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "Email Service API",
    version = "v1",
    timestamp = DateTime.UtcNow
}));

// Root endpoint for production
app.MapGet("/", () => Results.Ok(new
{
    service = "Email Service API",
    version = "v1",
    status = "running",
    endpoints = new[]
    {
        "GET /health - Health check",
        "POST /api/email/send-with-template - Send email with template",
        "POST /api/email/send-with-documents - Send email with documents",
        "GET /api/email/accounts - Get email accounts",
        "POST /api/emailtemplates - Create email template",
        "GET /api/emailtemplates - Get all email templates",
        "GET /api/emailtemplates/{id} - Get email template by ID",
        "PUT /api/emailtemplates/{id} - Update email template",
        "DELETE /api/emailtemplates/{id} - Delete email template",
        "POST /api/emailtemplates/{id}/activate - Activate email template",
        "POST /api/emailtemplates/{id}/deactivate - Deactivate email template",
        "GET /api/emailtemplates/{id}/analytics - Get email template analytics"
    },
    swagger = app.Environment.IsDevelopment() ? "/swagger" : null
}));

app.MapControllers();

// ⚠️ No database initialization - EmailService is stateless

// Log startup information
app.Logger.LogInformation("🚀 Email Service API is starting...");
app.Logger.LogInformation("📋 Available endpoints:");
app.Logger.LogInformation("     POST /api/email/send-with-template - Send email with TMS template");
app.Logger.LogInformation("     POST /api/email/send-with-documents - Send email with CMS documents");
app.Logger.LogInformation("     GET  /api/email/accounts - Get available email accounts");
app.Logger.LogInformation("     GET  /api/email/health - Health check");
app.Logger.LogInformation("🔧 Swagger UI available at: /");

app.Run();
