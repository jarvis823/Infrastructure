using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;

namespace Nacencom.Infrastructure.UnitOfWork
{
    public interface IRetry
    {
        RetryPolicy GetRetryPolicy(ITransactionRequired command);
        AsyncRetryPolicy GetAsyncRetryPolicy(ITransactionRequired command);
    }

    public class Retry<T> : IRetry where T : IExceptionDetector, new()
    {
        private static readonly IExceptionDetector _exceptionDetector = new T();
        private readonly ILogger _logger;
        private readonly Type _contextType;
        private readonly int _retryCount;

        public Retry(ILogger logger, Type contextType, int retryCount)
        {
            _logger = logger;
            _contextType = contextType;
            _retryCount = retryCount;
        }

        public virtual RetryPolicy GetRetryPolicy(ITransactionRequired command)
        {
            return Policy.Handle<Exception>(ex => ShouldRetryOn(ex)).WaitAndRetry(_retryCount, Sleep, (ex, t) => OnRetry(ex, t, command));
        }

        public virtual AsyncRetryPolicy GetAsyncRetryPolicy(ITransactionRequired command)
        {
            return Policy.Handle<Exception>(ex => ShouldRetryOn(ex)).WaitAndRetryAsync(_retryCount, Sleep, (ex, t) => OnRetry(ex, t, command));
        }

        public virtual TimeSpan Sleep(int retryAttempt)
        {
            var retryTime = Math.Pow(2, retryAttempt);
            if (retryTime > 60)
                retryTime = 60;
            return TimeSpan.FromSeconds(retryTime);
        }

        public virtual bool ShouldRetryOn(Exception ex)
        {
            return _exceptionDetector.ShouldRetryOn(ex);
        }

        public virtual void OnRetry(Exception ex, TimeSpan t, ITransactionRequired command)
        {
            _logger.LogWarning(ex, "{Context} {Command} Retries after {TimeOut}s ({ExceptionMessage})", _contextType.Name, command.GetType().Name, $"{t.TotalSeconds:n1}", ex.Message);
        }
    }
}
