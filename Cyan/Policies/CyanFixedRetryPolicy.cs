using System;
using System.Collections.Generic;

namespace Cyan.Policies
{
    public class CyanFixedRetryPolicy : CyanRetryPolicy
    {
        private readonly TimeSpan _interval;
        private readonly int _retries;

        public CyanFixedRetryPolicy(int retries, TimeSpan interval)
        {
            _retries = retries;
            _interval = interval;
        }

        public TimeSpan Interval
        {
            get { return _interval; }
        }

        public int Retries
        {
            get { return _retries; }
        }

        public override IEnumerable<TimeSpan> GetRetries()
        {
            for (var i = 0; i < _retries; i++)
                yield return _interval;
        }
    }
}