namespace Nacencom.Infrastructure.UnitOfWork
{
    public interface ITransactionRequired
    {
        bool TransactionRequired { get; }
    }
}
