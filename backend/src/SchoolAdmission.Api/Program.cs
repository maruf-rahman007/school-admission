using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Settings are read from the final configuration (env vars over appsettings.json defaults).
builder.Services.AddSingleton(sp => AppSettingsLoader.Load(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton(sp => sp.GetRequiredService<AppSettings>().Hold);

// CORS origins come from the validated settings, resolved lazily so test overrides apply.
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>().Configure<AppSettings>((options, settings) =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins([.. settings.CorsAllowedOrigins])
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders(ServerTimeMiddleware.HeaderName)));

builder.Services.AddControllers();

// Liveness only: no dependency checks (e.g. no database), so /health is 200 while the process runs.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Fail fast: invalid settings throw here, in every environment, before the server starts listening.
app.Services.GetRequiredService<AppSettings>();

// Order matters: the server-time header is registered first so every response gets it, preflight included.
app.UseMiddleware<ServerTimeMiddleware>();
app.UseCors();
app.UseRouting();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(
            JsonSerializer.Serialize(new { status = report.Status.ToString() }));
    },
});

app.Run();
