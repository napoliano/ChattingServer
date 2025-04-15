using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public interface IChannelItem
    {
        public Task ExecuteAsync();
    }


    public class SessionChannelItem : IChannelItem
    {
        private readonly Func<Task> _func;


        public SessionChannelItem(Func<Task> func)
        {
            _func = func;
        }

        public async Task ExecuteAsync()
        {
            await _func();
        }
    }


    public class ChatRoomGroupChannelItem<T> : IChannelItem
    {
        private readonly Func<Task<T>> _func;
        private readonly TaskCompletionSource<T> _tcs;


        public ChatRoomGroupChannelItem(Func<Task<T>> func, TaskCompletionSource<T> tcs)
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
