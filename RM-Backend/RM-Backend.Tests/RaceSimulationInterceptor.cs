using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RM_Backend.Tests
{
    /// <summary>
    /// Runs a one-time action right before the next SaveChanges - after the service has
    /// loaded the row and passed its manual Version check, but before the UPDATE is sent.
    /// This is exactly the race window that only the DB-level concurrency token can catch.
    /// </summary>
    public class RaceSimulationInterceptor : SaveChangesInterceptor
    {
        private Func<Task>? _beforeNextSave;

        public void RunOnceBeforeNextSave(Func<Task> action) => _beforeNextSave = action;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var action = Interlocked.Exchange(ref _beforeNextSave, null);
            if (action != null)
            {
                await action();
            }

            return result;
        }
    }
}
