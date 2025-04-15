using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public class RetryThresholdCounter
    {
        public int RetryCount => _retryCount;
        private int _retryCount = 0;

        private readonly int _threshold;
        private readonly Action? _onThresholdExceeded;


        public RetryThresholdCounter(int threshold, Action? onThresholdExceeded)
        {
            _threshold = threshold;
            _onThresholdExceeded = onThresholdExceeded;
        }

        public void Add()
        {
            ++_retryCount;
            if (_retryCount > _threshold)
            {
                _onThresholdExceeded?.Invoke();
            }
        }

        public void Reset()
        {
            _retryCount = 0;
        }
    }
}
