namespace TemplateMs.Application.Common.Interfaces;

/// <summary>Provides the current UTC date and time. Abstracts <see cref="DateTime.UtcNow"/> for testability.</summary>
public interface IDateTimeProvider
{
    /// <summary>Gets the current date and time in UTC.</summary>
    DateTime UtcNow { get; }
}
