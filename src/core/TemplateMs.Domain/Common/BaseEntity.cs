using TemplateMs.Domain.Common.Interfaces;

namespace TemplateMs.Domain.Common;

/// <summary>Base class for all domain entities providing identity and equality semantics.</summary>
public abstract class BaseEntity : IEntity
{
    /// <summary>Gets the unique identifier for this entity.</summary>
    public int Id { get; set; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>Equality operator.</summary>
    public static bool operator ==(BaseEntity? left, BaseEntity? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !(left == right);
}
