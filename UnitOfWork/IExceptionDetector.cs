using System;

namespace Nacencom.Infrastructure.UnitOfWork
{
    public interface IExceptionDetector
    {
        bool ShouldRetryOn(Exception ex);
    }
}