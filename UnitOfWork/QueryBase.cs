﻿using Dapper;
using Nacencom.Infrastructure.DataTypes;
using System.Data;

namespace Nacencom.Infrastructure.UnitOfWork
{
    public abstract class QueryBase<T> : CommandQuery, IQuery<T>, IQueryAsync<T>
    {
        public virtual bool TransactionRequired => false;

        public virtual T Query(IDbConnection connection, IDbTransaction transaction)
        {
            return connection.QueryFirstOrDefault<T>(
                CommandText
                , param: GetParams()
                , transaction: transaction
                , commandTimeout: CommandTimeout
                , commandType: CommandType
            );
        }

        public virtual async Task<T> QueryAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            return await connection.QueryFirstOrDefaultAsync<T>(
                CommandText
                , param: GetParams()
                , transaction: transaction
                , commandTimeout: CommandTimeout
                , commandType: CommandType
            );
        }
    }

    public abstract class QueryListBase<T> : CommandQuery, IQuery<List<T>>, IQueryAsync<List<T>>
    {
        public virtual bool TransactionRequired => false;

        public virtual List<T> Query(IDbConnection connection, IDbTransaction transaction)
        {
            return connection.Query<T>(
                CommandText
                , param: GetParams()
                , transaction: transaction
                , commandTimeout: CommandTimeout
                , commandType: CommandType
            ).AsList();
        }

        public virtual async Task<List<T>> QueryAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var result = await connection.QueryAsync<T>(
                CommandText
                , GetParams()
                , transaction: transaction
                , commandTimeout: CommandTimeout
                , commandType: CommandType
            );
            return result.AsList();
        }
    }

    public abstract class QueryPagedBase<T> : CommandQuery, IQuery<PagedList<T>>, IQueryAsync<PagedList<T>>
    {
        public virtual bool TransactionRequired => false;

        protected virtual string FieldCount => "COUNT";

        [Parameter]
        public int Page { get; set; }

        [Parameter]
        public int PageSize { get; set; }

        public virtual PagedList<T> Query(IDbConnection connection, IDbTransaction transaction)
        {
            var result = connection.Query<T, int, (T Item, int Count)>(
                CommandText
                , (a, b) => (a, b)
                , GetParams()
                , transaction: transaction
                , commandTimeout: CommandTimeout
                , commandType: CommandType
                , splitOn: FieldCount
            );
            return ToPagedList(result);
        }

        public virtual async Task<PagedList<T>> QueryAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var result = await connection.QueryAsync<T, int, (T Item, int Count)>(
                CommandText
                , (a, b) => (a, b)
                , GetParams()
                , transaction: transaction
                , commandTimeout: CommandTimeout
                , commandType: CommandType
                , splitOn: FieldCount
            );
            return ToPagedList(result!);
        }

        private PagedList<T> ToPagedList(IEnumerable<(T Item, int Count)> result)
        {
            return new PagedList<T>
            {
                PageSize = PageSize,
                Page = Page,
                TotalRecords = result.Select(t => t.Count).FirstOrDefault(),
                Items = result.Select(t => t.Item).ToList()
            };
        }
    }
}
