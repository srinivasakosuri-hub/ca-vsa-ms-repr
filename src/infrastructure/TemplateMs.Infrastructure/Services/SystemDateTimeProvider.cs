using TemplateMs.Application.Common.Interfaces;

namespace TemplateMs.Infrastructure.Services;

/// <summary>Production implementation of <see cref="IDateTimeProvider"/> that returns <see cref="DateTime.UtcNow"/>.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
