using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace Nacencom.Infrastructure.UnitOfWork
{
    public abstract class UnitOfWorkBase : IUnitOfWork
    {
        private readonly DbProviderFactory _dbProviderFactory;
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; private set; }
        public IsolationLevel IsolationLevel { get; }
        private readonly IRetry _retry;

        private readonly CancellationToken _cancellationToken = default;
        private bool _disposed;
        private readonly ILogger _logger;
        private readonly Type _type;

        public UnitOfWorkBase(IServiceProvider serviceProvider, UnitOfWorkOptions options)
        {
            OnConfiguring(serviceProvider.GetRequiredService<IConfiguration>(), options);
            _dbProviderFactory = options.DbProviderFactory;
            IsolationLevel = options.IsolationLevel;
            Connection = BuildConnection(options.ConnectionString);
            _logger = serviceProvider.GetRequiredService<ILogger<UnitOfWorkBase>>();
            _retry = options.RetryPolicy;
            _type = GetType();
        }

        protected virtual void OnConfiguring(IConfiguration configuration, UnitOfWorkOptions options)
        {

        }

        private DbConnection BuildConnection(string connectionString)
        {
            var connection = _dbProviderFactory.CreateConnection() ??
                throw new Exception("Error initializing connection");
            connection.ConnectionString = connectionString;
            return connection;
        }

        public void Commit()
        {
            try
            {
                if (Transaction != null)
                {
                    Transaction.Commit();
                    Transaction.Dispose();
                    Transaction = null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(Commit)} EXCEPTION", ex);
            }
        }

        public void OpenConnection() => Connection.Open();

        public async Task OpenConnectionAsync() => await Connection.OpenAsync();

        public Task CommitAsync() => CommitAsync(_cancellationToken);

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Transaction != null)
                {
                    await Transaction.CommitAsync(cancellationToken);
                    Transaction.Dispose();
                    Transaction = null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(CommitAsync)} EXCEPTION", ex);
            }
        }

        public void Rollback() => Transaction?.Rollback();

        public Task RollbackAsync() => RollbackAsync(_cancellationToken);

        public async Task RollbackAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Transaction != null)
                {
                    await Transaction.RollbackAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(RollbackAsync)} EXCEPTION", ex);
            }
        }

        public T Query<T>(IQuery<T> query)
        {
            try
            {
                TransactionRequired(query);
                var policy = _retry.GetRetryPolicy(query);
                return policy.Execute(() => query.Query(Connection, Transaction));
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(Query)} {query.GetType().Name} EXCEPTION", ex);
            }
        }

        public Task<T> QueryAsync<T>(IQueryAsync<T> query) => QueryAsync(query, _cancellationToken);

        public async Task<T> QueryAsync<T>(IQueryAsync<T> query, CancellationToken cancellationToken)
        {
            try
            {
                await TransactionRequiredAsync(query, cancellationToken);
                var policy = _retry.GetAsyncRetryPolicy(query);
                return await policy.ExecuteAsync(() => query.QueryAsync(Connection, Transaction, cancellationToken));
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(QueryAsync)} {query.GetType().Name} EXCEPTION", ex);
            }
        }

        public void Execute(ICommand command)
        {
            try
            {
                TransactionRequired(command);
                var policy = _retry.GetRetryPolicy(command);
                policy.Execute(() => command.Execute(Connection, Transaction));
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(Execute)} {command.GetType().Name} EXCEPTION", ex);
            }
        }

        public T Execute<T>(ICommand<T> command)
        {
            try
            {
                TransactionRequired(command);
                var policy = _retry.GetRetryPolicy(command);
                return policy.Execute(() => command.Execute(Connection, Transaction));
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(Execute)} {command.GetType().Name} EXCEPTION", ex);
            }
        }

        public Task ExecuteAsync(ICommandAsync command) => ExecuteAsync(command, _cancellationToken);

        public async Task ExecuteAsync(ICommandAsync command, CancellationToken cancellationToken)
        {
            try
            {
                await TransactionRequiredAsync(command, cancellationToken);
                var policy = _retry.GetAsyncRetryPolicy(command);
                await policy.ExecuteAsync(() => command.ExecuteAsync(Connection, Transaction, cancellationToken));
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(ExecuteAsync)} {command.GetType().Name} EXCEPTION", ex);
            }
        }

        public Task<T> ExecuteAsync<T>(ICommandAsync<T> command) => ExecuteAsync(command, _cancellationToken);

        public async Task<T> ExecuteAsync<T>(ICommandAsync<T> command, CancellationToken cancellationToken)
        {
            try
            {
                await TransactionRequiredAsync(command, cancellationToken);
                var policy = _retry.GetAsyncRetryPolicy(command);
                return await policy.ExecuteAsync(() => command.ExecuteAsync(Connection, Transaction, cancellationToken));
            }
            catch (Exception ex)
            {
                throw new Exception($"{_type.Name} {nameof(ExecuteAsync)} {command.GetType().Name} EXCEPTION", ex);
            }
        }

        protected virtual bool TransactionRequired(ITransactionRequired command)
        {
            if (command.TransactionRequired && Transaction == null)
            {
                if (Connection.State == ConnectionState.Closed)
                    Connection.Open();
                Transaction = Connection.BeginTransaction(IsolationLevel);
            }
            return true;
        }

        protected virtual async Task<bool> TransactionRequiredAsync(ITransactionRequired command, CancellationToken cancellationToken)
        {
            if (command.TransactionRequired && Transaction == null)
            {
                if (Connection.State == ConnectionState.Closed)
                    await Connection.OpenAsync(cancellationToken);
                Transaction = await Connection.BeginTransactionAsync(IsolationLevel, cancellationToken);
            }
            return true;
        }

        #region Finalizers

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UnitOfWorkBase() => Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Transaction?.Dispose();
                Connection?.Dispose();
            }
            _disposed = true;
        }

        #endregion Finalizers
    }
}
