using System;
using System.Collections.Generic;

namespace Cyan.Policies
{
    public class CyanNoRetriesRetryPolicy : CyanRetryPolicy
    {
        public override bool ShouldRetry(Exception exception)
        {
            return false;
        }

        public override IEnumerable<TimeSpan> GetRetries()
        {
            yield break;
        }
    }
}