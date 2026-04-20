using System.Text;
using System.IO.Compression;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using CardDuel.ServerApi.Hubs;
using CardDuel.ServerApi.Services;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Logging with structured context
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/cardduel-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
});

builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<PlayCardRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCardRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme.\r\n\r\n Enter your token in the text input below.\r\n\r\nExample: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// Database
var dbConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=5432;Database=cardduel;User Id=postgres;Password=postgres;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dbConnection));

var signingKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    signingKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
        ?? throw new InvalidOperationException("Missing JWT_SIGNING_KEY env var or Jwt:SigningKey in config");
}
var issuer = builder.Configuration["Jwt:Issuer"] ?? "cardduel-server";
var audience = builder.Configuration["Jwt:Audience"] ?? "cardduel-clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/match"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddHealthChecks()
    .AddNpgSql(dbConnection, name: "postgresql", timeout: TimeSpan.FromSeconds(5))
    .AddRedis(builder.Configuration["SignalR:RedisConnectionString"] ?? "localhost:6379", name: "redis", timeout: TimeSpan.FromSeconds(5));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(builder =>
        builder.WithOrigins(
                "http://localhost:3000", "http://localhost:5173", "http://localhost:5000",
                "http://127.0.0.1:3000", "http://127.0.0.1:5173", "http://127.0.0.1:5000",
                "http://192.168.1.84:3000", "http://192.168.1.84:5173", "http://192.168.1.84:5000")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()));

var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.HandshakeTimeout = TimeSpan.FromSeconds(5);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.MaximumParallelInvocationsPerClient = 10;
});

if (builder.Configuration.GetValue<bool>("SignalR:UseRedisBackplane"))
{
    var redisConnection = builder.Configuration["SignalR:RedisConnectionString"];
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        signalRBuilder.AddStackExchangeRedis(redisConnection);
    }
}

builder.Services.AddScoped<ICardCatalogService, DbCardCatalogService>();
builder.Services.AddScoped<ICardManagementService, CardManagementService>();
builder.Services.AddScoped<IDeckRepository, DbDeckRepository>();
builder.Services.AddSingleton<IMatchService, InMemoryMatchService>();
builder.Services.AddSingleton<InMemoryTournamentStore>();
builder.Services.AddScoped<IRatingService, EloRatingService>();
builder.Services.AddScoped<IRatingDbService, DbRatingService>();
builder.Services.AddScoped<IDeckValidationService, DeckValidationService>();
builder.Services.AddScoped<IReconnectionService, ReconnectionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReplayPersistenceService, ReplayPersistenceService>();
builder.Services.AddScoped<IReplayValidationService, ReplayValidationService>();

var app = builder.Build();

// Migrate database and seed
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        migrationLogger.LogInformation("Starting database migrations...");
        db.Database.Migrate();
        migrationLogger.LogInformation("Database migrations completed successfully");
        CardCatalogSeeder.SeedCards(db);
        migrationLogger.LogInformation("Card catalog seeded");
    }
    catch (Exception ex)
    {
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        migrationLogger.LogError(ex, "Failed to migrate or seed database");
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<MetricsMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCompression();

app.MapControllers();
app.MapHub<MatchHub>("/hubs/match");
app.MapHealthChecks("/api/v1/health");

// Prometheus metrics endpoint (no auth required)
app.MapGet("/metrics", () =>
{
    return Results.Text(PrometheusMetricsService.ExportMetrics(), "text/plain");
});

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("═══════════════════════════════════════════════════════════");
logger.LogInformation("🎮 CardDuel Server API RUNNING");
logger.LogInformation("═══════════════════════════════════════════════════════════");
logger.LogInformation("📍 API Base URL: http://0.0.0.0:5000 (or ASPNETCORE_URLS)");
logger.LogInformation("📚 Swagger Docs: http://0.0.0.0:5000/swagger");
logger.LogInformation("🎯 Match Hub: ws://0.0.0.0:5000/hubs/match (SignalR)");
logger.LogInformation("💚 Health Check: http://0.0.0.0:5000/api/v1/health");
logger.LogInformation("═══════════════════════════════════════════════════════════");

app.Run();
