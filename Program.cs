using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MISReports_Api.DBAccess;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers with NewtonsoftJson to maintain 100% serialization compatibility with Web API 2
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
    options.Conventions.Add(new MISReports_Api.Compatibility.WebApiRoutingConvention());
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new DefaultContractResolver(); // Preserve property names as in Models
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
});

// Configure CORS to allow frontend applications
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MIS Reports API",
        Version = "v1",
        Description = "CEB MIS Reports Web API - ASP.NET Core 10 (.NET 10 LTS) / C# 14"
    });
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

// Register Database Services & Helpers
builder.Services.AddSingleton<DBConnection>();

var app = builder.Build();

// Global Exception Handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            var errorResponse = JsonConvert.SerializeObject(new
            {
                success = false,
                message = "An internal server error occurred.",
                error = ex.Message,
                stackTrace = app.Environment.IsDevelopment() ? ex.StackTrace : null
            });
            await context.Response.WriteAsync(errorResponse);
        }
    }
});

// Enable Swagger UI in all environments for testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MIS Reports API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseCors();

app.MapControllers();

// Health / root endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "CEB MIS Reports API",
    version = "1.0.0 (.NET 10 / C# 14)",
    status = "Healthy",
    timestamp = DateTime.UtcNow
}));

app.Run();
