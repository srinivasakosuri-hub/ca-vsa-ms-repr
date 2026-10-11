namespace TemplateMs.Domain.Common.Interfaces;

/// <summary>
/// Represents an auditable entity with creation and update tracking.
/// </summary>
public interface IAuditableEntity : IEntity
{
    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    Guid CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    Guid UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Marks the entity as created by the specified user.
    /// </summary>
    /// <param name="userId">The identifier of the user who created the entity.</param>
    /// <param name="timestamp">The optional timestamp for creation and update.</param>
    void MarkAsCreated(Guid userId, DateTime? timestamp = null);

    /// <summary>
    /// Marks the entity as updated by the specified user.
    /// </summary>
    /// <param name="userId">The identifier of the user who updated the entity.</param>
    /// <param name="timestamp">The optional timestamp for update.</param>
    void MarkAsUpdated(Guid userId, DateTime? timestamp = null);
}