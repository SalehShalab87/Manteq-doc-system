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

// Build database connection string from environment variables
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "YOUR_SERVER\\SQLEXPRESS";
var dbDatabase = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "CmsDatabase_Dev";
var dbIntegratedSecurity = Environment.GetEnvironmentVariable("DB_INTEGRATED_SECURITY") ?? "true";
var dbTrustServerCertificate = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "true";

var connectionString = $"Data Source={dbServer};Initial Catalog={dbDatabase};Integrated Security={dbIntegratedSecurity};Persist Security Info=False;TrustServerCertificate={dbTrustServerCertificate};";

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework
builder.Services.AddDbContext<CmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add custom services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICmsTemplateService, CmsTemplateService>();

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

// Add CORS for development
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS Web API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
