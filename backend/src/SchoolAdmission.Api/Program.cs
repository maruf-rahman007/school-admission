using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SchoolAdmission.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Settings are read from the final configuration (env vars over appsettings.json defaults).
builder.Services.AddSingleton(sp => AppSettingsLoader.Load(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton(sp => sp.GetRequiredService<AppSettings>().Hold);

builder.Services.AddControllers();

// Liveness only: no dependency checks (e.g. no database), so /health is 200 while the process runs.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Fail fast: invalid settings throw here, in every environment, before the server starts listening.
app.Services.GetRequiredService<AppSettings>();

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
