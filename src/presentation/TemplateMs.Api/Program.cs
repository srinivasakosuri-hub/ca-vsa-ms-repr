using FastEndpoints;
using Scalar.AspNetCore;
using TemplateMs.Api.DependencyInjection;
using TemplateMs.Api.Filters;
using TemplateMs.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Logging 
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Services 
builder.Services.AddFastEndpoints();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "TemplateMs API";
    return Task.CompletedTask;
}));

builder.Services.AddExceptionHandler<GlobalExceptionFilter>();
builder.Services.AddProblemDetails();

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication(builder.Configuration)  // extension on IServiceCollection
    .AddMessageBusServices(builder.Configuration);

// Add authorization services (required for UseAuthorization() in pipeline)
builder.Services.AddAuthorization();

builder.AddWolverineMessaging();

// Build 
var app = builder.Build();

// Middleware pipeline 
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseFastEndpoints();

// Scalar API docs
app.MapOpenApi();
app.MapScalarApiReference(opts =>
{
    opts.Title = "TemplateMs API";
    opts.Theme = ScalarTheme.Purple;
});

app.Run();


/// <summary>
/// This is the entry point of the TemplateMs API application. It sets up the web host, configures services and middleware, and starts the application. 
/// The Program class is defined as partial to allow test projects to reference it for integration testing with WebApplicationFactory.
/// </summary>
public partial class Program { }
