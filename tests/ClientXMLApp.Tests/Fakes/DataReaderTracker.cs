using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace ClientXMLApp.Tests.Fakes
{
    public class DataReaderTracker : DbCommandInterceptor
    {
        public int Opened { get; private set; }

        public int Disposed { get; private set; }

        public int Open => Opened - Disposed;

        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            Opened++;
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            Opened++;
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult DataReaderDisposing(
            DbCommand command,
            DataReaderDisposingEventData eventData,
            InterceptionResult result)
        {
            Disposed++;
            return base.DataReaderDisposing(command, eventData, result);
        }
    }
}
