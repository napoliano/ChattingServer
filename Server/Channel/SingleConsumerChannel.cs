using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Runtime.CompilerServices;


namespace Server
{
    public class SingleConsumerChannel
    {
        private readonly Channel<IChannelItem> _channel = Channel.CreateUnbounded<IChannelItem>(new UnboundedChannelOptions { SingleReader = true });
        private readonly CancellationTokenSource _cts = new();

        private readonly Task _consumerTask;


        public SingleConsumerChannel()
        {
            _consumerTask = ConsumeItemAsync();
        }

        private async Task ConsumeItemAsync()
        {
            try
            {
                await foreach (var item in _channel.Reader.ReadAllAsync(_cts.Token))
                {
                    await item.ExecuteAsync();
                }
            }
            //채널이 취소된 경우
            catch (OperationCanceledException)
            {
                Log.Debug($"ConsumeItemAsync canceled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProcessChannelItemAsync failed");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryWrite(IChannelItem item)
        {
            _channel.Writer.TryWrite(item);
        }

        public void Close()
        {
            _channel.Writer.TryComplete();
            _cts.Cancel();
        }
    }
}
