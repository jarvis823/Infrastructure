using Dapper;
using System.Data;

namespace Nacencom.Infrastructure.UnitOfWork
{
    public abstract class CommandBase : CommandQuery, ICommand, ICommandAsync
    {
        protected override int? CommandTimeout => 600; //default 5mins

        public virtual bool TransactionRequired => true;

        public virtual void Execute(IDbConnection connection, IDbTransaction transaction)
        {
            connection.Execute(CommandText, GetParams(), transaction, CommandTimeout, CommandType);
        }

        public virtual async Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            await connection.ExecuteAsync(CommandText, GetParams(), transaction, CommandTimeout, CommandType);
        }
    }

    public abstract class CommandBase<T> : CommandQuery, ICommand<T>, ICommandAsync<T>
    {
        protected override int? CommandTimeout => 600; //default 5mins

        public virtual bool TransactionRequired => true;

        public virtual T Execute(IDbConnection connection, IDbTransaction transaction)
        {
            return connection.ExecuteScalar<T>(CommandText, GetParams(), transaction, CommandTimeout, CommandType);
        }

        public virtual async Task<T> ExecuteAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            return await connection.ExecuteScalarAsync<T>(CommandText, GetParams(), transaction, CommandTimeout, CommandType);
        }
    }
}
