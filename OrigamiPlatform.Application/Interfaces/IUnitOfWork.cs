namespace OrigamiPlatform.Application.Interfaces;

/// <summary>
/// Wraps a multi-repository write in a single DB transaction: the whole action commits together,
/// or (on an unhandled exception from inside it) rolls back together.
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}
