using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace Server
{
    public class ChatRoomGroup
    {
        private readonly int _groupId;

        private readonly Dictionary<int, ChatRoom> _chatRooms = new();

        private readonly CancellationTokenSource _cts = new();
        private readonly Channel<IChannelJob> _channel = Channel.CreateUnbounded<IChannelJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });


        public ChatRoomGroup(int groupId)
        {
            _groupId = groupId;

            _ = RunEventLoopAsync();
        }

        private async Task RunEventLoopAsync()
        {
            try
            {
                await foreach (var job in _channel.Reader.ReadAllAsync(_cts.Token))
                {
                    await job.ExecuteAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"RunEventLoopAsync failed - groupId:{_groupId}");
            }
        }


        public Task<(bool Success, ServerErrorCode Error)> CreateChatRoomAsync(int roomId, string title)
        {
            var tcs = new TaskCompletionSource<(bool, ServerErrorCode)>();

            _channel.Writer.TryWrite(new ChannelJob<(bool, ServerErrorCode)>(() =>
            {
                if (_chatRooms.ContainsKey(roomId) == false)
                {
                    _chatRooms[roomId] = new ChatRoom(title);
                    return Task.FromResult((true, ServerErrorCode.NONE));
                }

                return Task.FromResult((false, ServerErrorCode.RoomIdAlreadyExists));
            }, tcs));

            return tcs.Task;
        }

        public Task<(bool Success, ServerErrorCode Error)> JoinChatRoomAsync(int roomId, IParticipant participant)
        {
            var tcs = new TaskCompletionSource<(bool, ServerErrorCode)>();

            _channel.Writer.TryWrite(new ChannelJob<(bool, ServerErrorCode)>(() =>
            {
                if (_chatRooms.TryGetValue(roomId, out var chatRoom) == false)
                    return Task.FromResult((false, ServerErrorCode.ChatRoomNotFound));

                if (chatRoom.Join(participant) == false)
                    return Task.FromResult((false, ServerErrorCode.ChatRoomJoinFailed));

                return Task.FromResult((true, ServerErrorCode.NONE));
            }, tcs));

            return tcs.Task;
        }

        public Task<(bool Success, ServerErrorCode Error)> LeaveChatRoomAsync(int roomId, IParticipant participant)
        {
            var tcs = new TaskCompletionSource<(bool, ServerErrorCode)>();

            _channel.Writer.TryWrite(new ChannelJob<(bool, ServerErrorCode)>(() =>
            {
                if (_chatRooms.TryGetValue(roomId, out var chatRoom) == false)
                    return Task.FromResult((false, ServerErrorCode.ChatRoomNotFound));

                chatRoom.Leave(participant);

                if (chatRoom.IsEmpty())
                    _chatRooms.Remove(roomId);

                return Task.FromResult((true, ServerErrorCode.NONE));
            }, tcs));

            return tcs.Task;
        }
    }
}
