using System.Text;
using System.IO.Compression;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;
using CardDuel.ServerApi.Hubs;
using CardDuel.ServerApi.Services;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure.Swagger;

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
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CardDuel Server API",
        Version = "v1",
        Description = """
        Server-authoritative API for CardDuel.

        Recommended Swagger flow:
        1. Use the CardDuel Swagger Helper panel to register or login.
        2. Save the returned `playerId`, JWT token, deck ids, match ids, reconnect tokens, room codes, and ruleset ids in a local profile.
        3. Use the request examples. Placeholders like `{{playerId}}`, `{{deckId}}`, and `{{matchId}}` are replaced by the helper before the request is sent.
        4. For match snapshots, consume `battleEvents` in ascending `sequence`; that is the exact server-authoritative animation order.

        SignalR hub: `/hubs/match?matchId=<matchId>&access_token=<jwt>`.
        """,
        Contact = new OpenApiContact
        {
            Name = "CardDuel API"
        }
    });

    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    options.SupportNonNullableReferenceTypes();
    options.SchemaFilter<CardDuelSwaggerSchemaFilter>();
    options.OperationFilter<CardDuelSwaggerOperationFilter>();
    options.DocumentFilter<CardDuelSwaggerDocumentFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste only the JWT token. Swagger sends it as `Authorization: Bearer <token>`. The CardDuel helper can fill this automatically after login."
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

var useRedisBackplane = builder.Configuration.GetValue<bool>("SignalR:UseRedisBackplane");
var redisConnection = builder.Configuration["SignalR:RedisConnectionString"];

var healthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(dbConnection, name: "postgresql", timeout: TimeSpan.FromSeconds(5));

if (useRedisBackplane && !string.IsNullOrWhiteSpace(redisConnection))
{
    healthChecks.AddRedis(redisConnection, name: "redis", timeout: TimeSpan.FromSeconds(5));
}

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(builder =>
        builder.WithOrigins(
                "http://localhost",
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

if (useRedisBackplane)
{
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        signalRBuilder.AddStackExchangeRedis(redisConnection);
    }
}

builder.Services.AddScoped<ICardCatalogService, DbCardCatalogService>();
builder.Services.AddScoped<ICardManagementService, CardManagementService>();
builder.Services.AddScoped<IDeckRepository, DbDeckRepository>();
builder.Services.AddScoped<IGameRulesetService, GameRulesetService>();
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
        AuthoringSeeder.SeedAuthoringData(db);
        migrationLogger.LogInformation("Authoring lookup/template data seeded");
        GameRulesetSeeder.SeedDefaultRuleset(db);
        migrationLogger.LogInformation("Default game ruleset seeded");
    }
    catch (Exception ex)
    {
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        migrationLogger.LogError(ex, "Failed to migrate or seed database");
        throw;
    }
}

app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "CardDuel API Workbench";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CardDuel Server API v1");
    options.DocExpansion(DocExpansion.None);
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnablePersistAuthorization();
    options.ShowExtensions();
    options.ShowCommonExtensions();
    options.InjectStylesheet("/swagger-ui/cardduel-swagger.css");
    options.InjectJavascript("/swagger-ui/cardduel-swagger.js");
    options.UseRequestInterceptor("function(request) { if (window.cardDuelSwagger) { var token = window.cardDuelSwagger.getToken(); if (token && request.headers && !request.headers.Authorization) { request.headers.Authorization = 'Bearer ' + token; } request.url = window.cardDuelSwagger.replaceVariables(request.url); if (typeof request.body === 'string') { request.body = window.cardDuelSwagger.replaceVariables(request.body); } } return request; }");
    options.UseResponseInterceptor("function(response) { if (window.cardDuelSwagger && typeof window.cardDuelSwagger.handleAuthResponse === 'function') { return window.cardDuelSwagger.handleAuthResponse(response); } return response; }");
});
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<MetricsMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();
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

logger.LogInformation("SignalR Redis Backplane: {RedisBackplaneStatus}", useRedisBackplane ? "ENABLED" : "DISABLED");
if (useRedisBackplane)
{
    logger.LogInformation("SignalR Redis Connection: {RedisConnection}", string.IsNullOrWhiteSpace(redisConnection) ? "<missing>" : redisConnection);
}

app.Run();
