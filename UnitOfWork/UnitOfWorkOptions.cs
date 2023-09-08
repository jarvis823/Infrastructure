using System.Data;
using System.Data.Common;

namespace Nacencom.Infrastructure.UnitOfWork
{
    public class UnitOfWorkOptions
    {
        public string ConnectionString { get; set; } = default!;
        public DbProviderFactory DbProviderFactory { get; set; } = default!;
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
        public bool Transactional { get; set; } = true;
        public int RetryCount { get; set; } = 5;
        public bool IsConfigured { get; set; }
        public IRetry RetryPolicy { get; set; } = default!;
    }

    public class UnitOfWorkOptions<Tcontext> : UnitOfWorkOptions
        where Tcontext : UnitOfWorkBase
    {

    }
}
