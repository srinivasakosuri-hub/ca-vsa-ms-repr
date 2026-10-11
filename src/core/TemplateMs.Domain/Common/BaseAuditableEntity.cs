using TemplateMs.Domain.Common.Interfaces;

namespace TemplateMs.Domain.Common;

/// <summary>
/// Represents a base auditable entity with creation and update tracking.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity, IAuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public new int Id
    {
        get => base.Id;
        set => base.Id = value;
    }

    /// <inheritdoc/>
    public Guid CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public Guid UpdatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime UpdatedAt { get; set; }

    /// <inheritdoc/>
    public void MarkAsCreated(Guid userId, DateTime? timestamp = null)
    {
        var now = timestamp ?? DateTime.UtcNow;
        CreatedBy = UpdatedBy = userId;
        CreatedAt = UpdatedAt = now;
    }

    /// <inheritdoc/>
    public void MarkAsUpdated(Guid userId, DateTime? timestamp = null)
    {
        UpdatedBy = userId;
        UpdatedAt = timestamp ?? DateTime.UtcNow;
    }
}