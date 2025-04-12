using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public interface IChannelJob
    {
        public Task ExecuteAsync();
    }

    public class ChannelJob<T> : IChannelJob
    {
        private readonly Func<Task<T>> _func;
        private readonly TaskCompletionSource<T> _tcs;


        public ChannelJob(Func<Task<T>> func, TaskCompletionSource<T> tcs)
        {
            _func = func;
            _tcs = tcs;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                var result = await _func();
                _tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
        }
    }
}
