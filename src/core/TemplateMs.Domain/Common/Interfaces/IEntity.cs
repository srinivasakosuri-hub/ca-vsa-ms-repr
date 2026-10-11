namespace TemplateMs.Domain.Common.Interfaces
{
    /// <summary>
    /// Represents a base entity with a unique identifier.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        int Id { get; set; }
    }
}
