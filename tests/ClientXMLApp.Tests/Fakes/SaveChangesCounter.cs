using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClientXMLApp.Tests.Fakes
{
    // The rows land either way, so only the round trip count shows a batch insert is one save.
    public class SaveChangesCounter : SaveChangesInterceptor
    {
        public int Count { get; private set; }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            Count++;
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
