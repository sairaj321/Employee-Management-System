using System.Text.Json;
using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;

    public AuditService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public async Task LogAsync(
        int? userId, string action, string entityName, string entityId,
        object? oldValue, object? newValue, string? correlationId = null, string? ipAddress = null, CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue, JsonOptions) : null,
            NewValue = newValue != null ? JsonSerializer.Serialize(newValue, JsonOptions) : null,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        await _repository.AddAsync(log, ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogQueryDto query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            query.Page, query.PageSize, query.EntityName, query.UserId, query.From, query.To, ct);

        var dtos = items.Select(a => new AuditLogDto
        {
            Id = a.Id,
            UserId = a.UserId,
            UserEmail = a.User?.Email,
            Action = a.Action,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            IpAddress = a.IpAddress,
            CorrelationId = a.CorrelationId,
            Timestamp = a.Timestamp
        }).ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(INotificationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task NotifyAsync(int userId, string type, string title, string message, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly, CancellationToken ct = default)
    {
        var items = await _repository.GetUserNotificationsAsync(userId, unreadOnly, ct);
        return items.Select(n => new NotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default)
    {
        return await _repository.GetUnreadCountAsync(userId, ct);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId, CancellationToken ct = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, ct);
        if (notification != null && notification.UserId == userId)
        {
            notification.IsRead = true;
            _repository.Update(notification);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
