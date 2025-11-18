using Microsoft.EntityFrameworkCore;
using CMS.WebApi.Data;
using CMS.WebApi.Services;

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

// Build PostgreSQL connection string from environment variables or appsettings
string connectionString;

var pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST");
var pgPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var pgDatabase = Environment.GetEnvironmentVariable("POSTGRES_DB");
var pgUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
var pgPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
var pgSslMode = Environment.GetEnvironmentVariable("POSTGRES_SSL_MODE") ?? "Prefer";

if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgDatabase))
{
    // Build from environment variables (Docker/Production)
    connectionString = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword};SSL Mode={pgSslMode}";
    Console.WriteLine($"🔧 CMS using PostgreSQL: {pgHost}:{pgPort}/{pgDatabase}");
}
else
{
    // Use appsettings.json (Development)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string not configured");
    Console.WriteLine("🔧 CMS using connection string from appsettings.json");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<CmsDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        );
        npgsqlOptions.CommandTimeout(30);
    });
});

// Add custom services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICmsTemplateService, CmsTemplateService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailTemplateFileService, EmailTemplateFileService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "CMS Web API", 
        Version = "v1",
        Description = "Content Management System API for document storage and retrieval"
    });
    
    // Include XML comments for better API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS for Angular frontend
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS Web API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "CMS API",
    version = "v1",
    timestamp = DateTime.UtcNow
}));

// Root endpoint for production
app.MapGet("/", () => Results.Ok(new
{
    service = "Content Management System (CMS) API",
    version = "v1",
    status = "running",
    endpoints = new[]
    {
        "GET /health - Health check",
        "POST /api/documents/register - Register document",
        "GET /api/documents/{id} - Get document by ID",
        "GET /api/documents - Get all documents",
        "POST /api/documents/{id}/activate - Activate document",
        "POST /api/documents/{id}/deactivate - Deactivate document",
        "GET /api/documents/types - Get document types"
    },
    swagger = app.Environment.IsDevelopment() ? "/swagger" : null
}));

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
