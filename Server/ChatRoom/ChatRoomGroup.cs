﻿using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Server
{
    public class ChatRoomGroup
    {
        private readonly Dictionary<int, ChatRoom> _chatRooms = new();

        private readonly int _groupId;

        private readonly SingleConsumerChannel _channel = new();

        
        public ChatRoomGroup(int groupId)
        {
            _groupId = groupId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task<T> TryWriteToChannel<T>(Func<Task<T>> func)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _channel.TryWrite(new ChatRoomGroupChannelItem<T>(func, tcs));
            return tcs.Task;
        }

        /// <summary>
        /// 채팅 방 생성
        /// </summary>
        public Task<(bool success, ServerErrorCode errorCode)> CreateChatRoomAsync(int roomId, string title)
        {
            return TryWriteToChannel(() =>
            {
                //roomId가 중복된 경우
                if (_chatRooms.ContainsKey(roomId))
                    return Task.FromResult((false, ServerErrorCode.RoomIdAlreadyExists));

                _chatRooms[roomId] = new ChatRoom(title);
                return Task.FromResult((true, ServerErrorCode.None));
            });
        }

        /// <summary>
        /// 채팅 방 입장
        /// </summary>
        public Task<(bool success, ServerErrorCode errorCode)> JoinChatRoomAsync(int roomId, IParticipant participant)
        {
            return TryWriteToChannel(() =>
            {
                //roomId에 매칭되는 채팅 방이 없는 경우
                if (_chatRooms.TryGetValue(roomId, out var chatRoom) == false)
                    return Task.FromResult((false, ServerErrorCode.ChatRoomNotFound));

                var (suceess, errorCode) = chatRoom.Join(participant);
                return Task.FromResult((suceess, errorCode));
            });
        }

        /// <summary>
        /// 채팅 방 퇴장
        /// </summary>
        public Task<(bool success, ServerErrorCode errorCode)> LeaveChatRoomAsync(int roomId, IParticipant participant)
        {
            return TryWriteToChannel(() =>
            {
                //roomId에 매칭되는 채팅 방이 없는 경우
                if (_chatRooms.TryGetValue(roomId, out var chatRoom) == false)
                    return Task.FromResult((false, ServerErrorCode.ChatRoomNotFound));

                chatRoom.Leave(participant);

                //채팅 방에 유저가 없으면 채팅 방 제거
                if (chatRoom.IsEmpty())
                    _chatRooms.Remove(roomId);

                return Task.FromResult((true, ServerErrorCode.None));
            });
        }

        /// <summary>
        /// 채팅 메시지 전달
        /// </summary>
        public Task<(bool success, ServerErrorCode errorCode)> BroadcastMessageAsync(int roomId, ChatMessage chatMessage)
        {
            return TryWriteToChannel(() =>
            {
                //roomId에 매칭되는 채팅 방이 없는 경우
                if (_chatRooms.TryGetValue(roomId, out var chatRoom) == false)
                    return Task.FromResult((false, ServerErrorCode.ChatRoomNotFound));

                chatRoom.Broadcast(chatMessage);

                return Task.FromResult((true, ServerErrorCode.None));
            });
        }
    }
}
