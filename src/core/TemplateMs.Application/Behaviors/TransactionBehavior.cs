using TemplateMs.Application.Common.Interfaces;

namespace TemplateMs.Application.Behaviors;

/// <summary>
/// Wolverine middleware that wraps handler execution in a unit-of-work transaction.
/// Commits on success and rolls back on any exception, ensuring atomicity across
/// repository writes, domain event dispatch, and outbox persistence.
/// </summary>
public sealed class TransactionBehavior
{
    /// <summary>Wraps the next pipeline step in a transaction managed by <see cref="IUnitOfWork"/>.</summary>
    /// <param name="unitOfWork">The application unit of work (scoped per message).</param>
    /// <param name="next">The next step in the Wolverine pipeline.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public static async Task ExecuteAsync(
        IUnitOfWork unitOfWork,
        Func<Task> next,
        CancellationToken cancellationToken)
    {
        await next();
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
