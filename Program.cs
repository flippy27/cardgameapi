using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CardDuel.ServerApi.Hubs;
using CardDuel.ServerApi.Services;
using CardDuel.ServerApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var signingKey = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Missing Jwt:SigningKey");
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

var signalRBuilder = builder.Services.AddSignalR();
if (builder.Configuration.GetValue<bool>("SignalR:UseRedisBackplane"))
{
    var redisConnection = builder.Configuration["SignalR:RedisConnectionString"];
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        signalRBuilder.AddStackExchangeRedis(redisConnection);
    }
}

builder.Services.AddSingleton<ICardCatalogService, InMemoryCardCatalogService>();
builder.Services.AddSingleton<IDeckRepository, InMemoryDeckRepository>();
builder.Services.AddSingleton<IMatchService, InMemoryMatchService>();
builder.Services.AddSingleton<InMemoryTournamentStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MatchHub>("/hubs/match");

app.Run();
