using System;
using System.Collections.Generic;

namespace Cyan.Policies
{
    public abstract class CyanRetryPolicy
    {
        public static readonly CyanRetryPolicy Default = new CyanNoRetriesRetryPolicy();

        public virtual bool ShouldRetry(Exception exception)
        {
            var cyanException = exception as CyanException;
            return cyanException == null || ShouldRetry(cyanException.ErrorCode);

            // should be a protocol error
        }

        public virtual bool ShouldRetry(string errorCode)
        {
            switch (errorCode)
            {
                case "InternalError":
                case "OperationTimedOut":
                case "ServerBusy":
                case "TableBeingDeleted":
                    return true;
                default:
                    return false;
            }
        }

        public abstract IEnumerable<TimeSpan> GetRetries();
    }
}