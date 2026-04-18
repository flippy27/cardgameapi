using System.Text.Json;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Services;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string resource, string resourceId, string? details = null, string? ipAddress = null, int? statusCode = null);
}

public sealed class AuditService(AppDbContext dbContext) : IAuditService
{
    public async Task LogAsync(string userId, string action, string resource, string resourceId, string? details = null, string? ipAddress = null, int? statusCode = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            Details = details ?? string.Empty,
            IpAddress = ipAddress ?? string.Empty,
            StatusCode = statusCode,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }
}
