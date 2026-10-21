using System.Text.Json;
using WebsiteAPI.Data;
using WebsiteAPI.Entities;

namespace WebsiteAPI.Services;

public sealed class AuditService(ApplicationDbContext context)
{
    public void Add(
        int? userId,
        string action,
        string entityType,
        string entityId,
        object? details = null,
        string? ipAddress = null)
    {
        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = JsonSerializer.SerializeToDocument(details ?? new { }),
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
    }
}
