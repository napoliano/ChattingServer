using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace Server
{
    public class ChatRoomGroup
    {
        private readonly Dictionary<int, ChatRoom> _chatRooms = new();

        private readonly Channel<Action> _channel = Channel.CreateUnbounded<Action>();
        private readonly CancellationTokenSource _cancellationTokenSource = new();


        public void CreateChatRoom(int roomId, string title, Action<bool, ServerErrorCode> response)
        {
            _channel.Writer.TryWrite(() =>
            {
                if (_chatRooms.ContainsKey(roomId) == false)
                {
                    _chatRooms[roomId] = new ChatRoom(title);
                    response?.Invoke(true, ServerErrorCode.NONE);
                }
                else
                {
                    response?.Invoke(false, ServerErrorCode.RoomIdAlreadyExists);
                }
            });
        }

        public void JoinChatRoom(int roomId, IParticipant participant, Action<bool, ServerErrorCode> response)
        {
            _channel.Writer.TryWrite(() =>
            {
                if (_chatRooms.TryGetValue(roomId, out var chatRoom) == false)
                {
                    response?.Invoke(false, ServerErrorCode.ChatRoomNotFound);
                    return;
                }

                if (chatRoom.Join(participant) == false)
                {
                    response?.Invoke(false, ServerErrorCode.ChatRoomJoinFailed);
                    return;
                }

                response?.Invoke(true, ServerErrorCode.NONE);
            });
        }

        public void LeaveChatRoom(int roomId, IParticipant participant, Action<bool, ServerErrorCode> response)
        {
            _channel.Writer.TryWrite(() =>
            {
                if (_chatRooms.TryGetValue(roomId, out var chatRoom) == false)
                {
                    response?.Invoke(false, ServerErrorCode.ChatRoomNotFound);
                    return;
                }

                chatRoom.Leave(participant);

                response?.Invoke(true, ServerErrorCode.NONE);
            });
        }

        //private async Task RunEventLoopAsync()
        //{
        //    try
        //    {
        //        await foreach (var action in _channel.Reader.ReadAllAsync(_cancellationToken.Token))
        //            action?.Invoke();
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"RunEventLoopAsync failed - Id:{_id}");
        //    }
        //}
    }
}
