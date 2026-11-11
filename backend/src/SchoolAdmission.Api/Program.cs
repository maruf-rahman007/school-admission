using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Errors;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Infrastructure;
using SchoolAdmission.Infrastructure.Persistence;

// An invalid configuration or a failed migration (DatabaseMigrator runs before the server listens) ends the
// process with its message and exit code 1 instead of a crash.
// In-process test hosts catch the exception themselves, so there this handler never runs.
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    if (e.ExceptionObject is InvalidConfigurationException or DatabaseMigrationException)
    {
        Console.Error.WriteLine(((Exception)e.ExceptionObject).Message);
        Environment.Exit(1);
    }
};

var builder = WebApplication.CreateBuilder(args);

// Settings are read from the final configuration (env vars over appsettings.json defaults).
builder.Services.AddSingleton(sp => AppSettingsLoader.Load(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton(sp => sp.GetRequiredService<AppSettings>().Hold);
builder.Services.AddInfrastructure(sp => sp.GetRequiredService<AppSettings>().ConnectionString);

// CORS origins come from the validated settings, resolved lazily so test overrides apply.
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>().Configure<AppSettings>((options, settings) =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins([.. settings.CorsAllowedOrigins])
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders(ServerTimeMiddleware.HeaderName)));

// No ProblemDetails anywhere: errors use our own body (STANDARDS §5).
builder.Services.AddControllers(options =>
        // Missing fields are reported by FluentValidation (VALIDATION_FAILED), not by implicit [Required].
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressMapClientErrors = true;
        options.InvalidModelStateResponseFactory = FrameworkErrors.MalformedRequest;
    });

// OpenAPI document (built-in generator) + Swagger UI, on in every environment so the API can be explored.
builder.Services.AddOpenApi();

// Liveness only: no dependency checks (e.g. no database), so /health is 200 while the process runs.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Fail fast: invalid settings throw here, in every environment, before the server starts listening.
app.Services.GetRequiredService<AppSettings>();


// Order matters: the server-time header is registered first so every response gets it, preflight included.
app.UseMiddleware<ServerTimeMiddleware>();
app.UseCors();
// After CORS so error responses still carry the CORS headers the browser needs to read them.
app.UseMiddleware<ErrorHandlingMiddleware>();
// Empty 4xx/5xx (unknown route, wrong method, wrong content type, bare status results) get the standard body.
app.UseStatusCodePages(FrameworkErrors.WriteForStatusCode);
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.SwaggerEndpoint("/openapi/v1.json", "School Admission API v1");
});
app.UseRouting();

app.MapControllers();
app.MapOpenApi();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(
            JsonSerializer.Serialize(new { status = report.Status.ToString() }));
    },
}).ExcludeFromDescription();

app.Run();
