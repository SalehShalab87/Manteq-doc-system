using CMS.WebApi.Data;
using CMS.WebApi.Services;
using TMS.WebApi.Services;
using TMS.WebApi.Infrastructure;
using TMS.WebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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
    // Use SQL Server connection string from CMS configuration
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                         ?? "Server=(localdb)\\mssqllocaldb;Database=ManteqCmsDb;Trusted_Connection=true;MultipleActiveResultSets=true";
    options.UseSqlServer(connectionString);
});

// Register CMS Services (used internally by TMS - no CMS endpoints are exposed)
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICmsTemplateService, CmsTemplateService>();

// Register TMS Services (these power the exposed TMS API endpoints)
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
builder.Services.AddScoped<IDocumentEmbeddingService, DocumentEmbeddingService>();

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

// Configure CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TMS API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

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
app.Logger.LogInformation("   GET  /api/templates/{id} - Retrieve template");
app.Logger.LogInformation("   GET  /api/templates/{id}/properties - Get template properties");
app.Logger.LogInformation("   POST /api/templates/generate - Generate document from template");
app.Logger.LogInformation("   POST /api/templates/generate-with-embeddings - Generate document with embeddings");
app.Logger.LogInformation("   GET  /api/templates/download/{id} - Download generated document");
app.Logger.LogInformation("🔧 Swagger UI available at: /");

app.Run();
