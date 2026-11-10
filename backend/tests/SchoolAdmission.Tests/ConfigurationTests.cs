using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Application.Common;

namespace SchoolAdmission.Tests;

// Overrides go through in-memory configuration; process-wide environment variables are never changed.
public class ConfigurationTests
{
    private const string DefaultConnectionString =
        "Host=db;Port=5432;Database=admission;Username=admission;Password=admission";

    [Fact]
    public void Startup_WithNoOverrides_UsesDocumentedDefaults()
    {
        var settings = Start(new Dictionary<string, string?>(), ignoreEnvironment: true);

        Assert.Equal(600, settings.Hold.HoldDurationSeconds);
        Assert.Equal(5, settings.Hold.ExpirySweepIntervalSeconds);
        Assert.Equal(["http://localhost:3000"], settings.CorsAllowedOrigins);
        Assert.Equal(DefaultConnectionString, settings.ConnectionString);
    }

    [Fact]
    public void Startup_WithHoldDurationOverride_BindsValue()
    {
        using var factory = CreateFactory(new() { ["HOLD_DURATION_SECONDS"] = "30" });

        Assert.Equal(30, factory.Services.GetRequiredService<HoldOptions>().HoldDurationSeconds);
    }

    [Fact]
    public void Startup_WithSweepIntervalOverride_BindsValue()
    {
        var settings = Start(new() { ["EXPIRY_SWEEP_INTERVAL_SECONDS"] = "2" });

        Assert.Equal(2, settings.Hold.ExpirySweepIntervalSeconds);
    }

    [Fact]
    public void Startup_WithMultipleCorsOrigins_BindsAll()
    {
        var settings = Start(new() { ["CORS_ALLOWED_ORIGINS"] = "http://localhost:3000,http://127.0.0.1:3000" });

        Assert.Equal(["http://localhost:3000", "http://127.0.0.1:3000"], settings.CorsAllowedOrigins);
    }

    [Fact]
    public void Startup_WithConnectionStringOverride_BindsValue()
    {
        var settings = Start(new() { ["ConnectionStrings:Default"] = "Host=x;Database=y" });

        Assert.Equal("Host=x;Database=y", settings.ConnectionString);
    }

    public static TheoryData<string> InvalidPositiveInts =>
        ["abc", "0", "-5", "1.5", "1e3", "৬০০", "2147483648", "", "   "];

    [Theory]
    [MemberData(nameof(InvalidPositiveInts))]
    public void Startup_WithInvalidHoldDuration_FailsNamingVariable(string value)
    {
        var error = StartExpectingFailure(new() { ["HOLD_DURATION_SECONDS"] = value });

        Assert.Contains("HOLD_DURATION_SECONDS", error.Message);
        Assert.Contains($"'{value}'", error.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidPositiveInts))]
    public void Startup_WithInvalidSweepInterval_FailsNamingVariable(string value)
    {
        var error = StartExpectingFailure(new() { ["EXPIRY_SWEEP_INTERVAL_SECONDS"] = value });

        Assert.Contains("EXPIRY_SWEEP_INTERVAL_SECONDS", error.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("localhost:3000")]
    [InlineData("ftp://x.com")]
    [InlineData("*")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://a.com/x")]
    [InlineData("http://a.com?q=1")]
    [InlineData("http://localhost:3000,not-a-url")]
    public void Startup_WithInvalidCorsOrigins_FailsNamingVariable(string value)
    {
        var error = StartExpectingFailure(new() { ["CORS_ALLOWED_ORIGINS"] = value });

        Assert.Contains("CORS_ALLOWED_ORIGINS", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Startup_WithEmptyConnectionString_FailsNamingVariable(string value)
    {
        var error = StartExpectingFailure(new() { ["ConnectionStrings:Default"] = value });

        Assert.Contains("ConnectionStrings__Default", error.Message);
        Assert.DoesNotContain(DefaultConnectionString, error.Message);
    }

    [Fact]
    public void Startup_WithTwoInvalidVariables_FailsNamingBoth()
    {
        var error = StartExpectingFailure(new()
        {
            ["HOLD_DURATION_SECONDS"] = "abc",
            ["CORS_ALLOWED_ORIGINS"] = "*",
        });

        Assert.Contains("HOLD_DURATION_SECONDS", error.Message);
        Assert.Contains("CORS_ALLOWED_ORIGINS", error.Message);
    }

    [Fact]
    public void Startup_WithCorsOriginsNeedingNormalisation_BindsNormalised()
    {
        var settings = Start(new() { ["CORS_ALLOWED_ORIGINS"] = " http://a.com, http://b.com ,http://localhost:3000/," });

        Assert.Equal(["http://a.com", "http://b.com", "http://localhost:3000"], settings.CorsAllowedOrigins);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("2147483647", int.MaxValue)]
    [InlineData(" 600 ", 600)]
    [InlineData("+600", 600)]
    [InlineData("0600", 600)]
    public void Startup_WithLenientIntegerFormats_AcceptsValue(string value, int expected)
    {
        var settings = Start(new() { ["HOLD_DURATION_SECONDS"] = value });

        Assert.Equal(expected, settings.Hold.HoldDurationSeconds);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Startup_WithInvalidSettingInAnyEnvironment_Fails(string environment)
    {
        using var factory = CreateFactory(new() { ["HOLD_DURATION_SECONDS"] = "0" })
            .WithWebHostBuilder(builder => builder.UseEnvironment(environment));

        AssertStartupFails(factory);
    }

    private static AppSettings Start(Dictionary<string, string?> overrides, bool ignoreEnvironment = false)
    {
        using var factory = CreateFactory(overrides, ignoreEnvironment);
        return factory.Services.GetRequiredService<AppSettings>();
    }

    private static InvalidConfigurationException StartExpectingFailure(Dictionary<string, string?> overrides)
    {
        using var factory = CreateFactory(overrides);
        return AssertStartupFails(factory);
    }

    private static InvalidConfigurationException AssertStartupFails(WebApplicationFactory<Program> factory)
    {
        var thrown = Assert.ThrowsAny<Exception>(() => factory.Services);
        for (Exception? e = thrown; e is not null; e = e.InnerException)
        {
            if (e is InvalidConfigurationException invalid)
            {
                return invalid;
            }
        }

        throw new Xunit.Sdk.XunitException($"Expected {nameof(InvalidConfigurationException)}, got: {thrown}");
    }

    private static WebApplicationFactory<Program> CreateFactory(
        Dictionary<string, string?> overrides, bool ignoreEnvironment = false) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
            {
                if (ignoreEnvironment)
                {
                    // The defaults test must not see variables set on the machine or in compose.
                    var envSources = config.Sources.OfType<EnvironmentVariablesConfigurationSource>().ToList();
                    envSources.ForEach(source => config.Sources.Remove(source));
                }

                config.AddInMemoryCollection(overrides);
            }));
}
