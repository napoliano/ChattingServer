using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;


namespace Server
{
    public class User : IParticipant
    {
        public int Id => _session.Id;

        public string Name => $"Guest_{Id}";

        public bool IsUser => true;

        private readonly ClientSession _session;

        private int _roomId;


        public User(ClientSession session)
        {
            _session = session;
        }

        public void ReceiveMessage(ChatMessage chatMessage)
        {
            //...
        }

        public async Task CreateChatRoomAysnc(string title)
        {
            //이미 참여 중인 채팅 방이 있는 경우
            if (_roomId != 0)
            {
                Log.Error($"Already joined in the chat room - Id:{Id}, roomId:{_roomId}");
                _session.SendResponse(PacketCommand.CsCreateChatRoomFailure, new CsCommonFailure() { errorCode = ServerErrorCode.AlreadyInChatRoom });
                return;
            }

            int roomId = ChatRoomGroupManager.Instance.GetNewRoomId();
            var chatRoomGroup = ChatRoomGroupManager.Instance.GetChatRoomGroup(roomId);
            
            var (success, errorCode) = await chatRoomGroup.CreateChatRoomAsync(roomId, title);
            if (success)
            {
                _roomId = roomId;
                _session.SendResponse(PacketCommand.CsCreateChatRoomSuccess, new CsCreateChatRoomSuccess());
            }
            else
            {
                _session.SendResponse(PacketCommand.CsCreateChatRoomFailure, new CsCommonFailure() { errorCode = errorCode });
            }
        }

        public async Task JoinChatRoomAysnc(int roomId)
        {
            //이미 참여 중인 채팅 방이 있는 경우
            if (_roomId != 0)
            {
                Log.Error($"Already joined in the chat room - Id:{Id}, roomId:{_roomId}");
                _session.SendResponse(PacketCommand.CsJoinChatRoomFailure, new CsCommonFailure() { errorCode = ServerErrorCode.AlreadyInChatRoom });
                return;
            }

            var chatRoomGroup = ChatRoomGroupManager.Instance.GetChatRoomGroup(roomId);

            var (success, errorCode) = await chatRoomGroup.JoinChatRoomAsync(roomId, this);
            if (success)
            {
                _roomId = roomId;
                _session.SendResponse(PacketCommand.CsJoinChatRoomSuccess, new CsJoinChatRoomSuccess());
            }
            else
            {
                _session.SendResponse(PacketCommand.CsJoinChatRoomFailure, new CsCommonFailure() { errorCode = errorCode });
            }
        }

        public async Task LeaveChatRoomAysnc()
        {
            //참여 중인 채팅 방이 없는 경우
            if (_roomId == 0)
            {
                Log.Error($"Not in any chat room - Id:{Id}");
                _session.SendResponse(PacketCommand.CsLeaveChatRoomFailure, new CsCommonFailure() { errorCode = ServerErrorCode.JoinedChatRoomNotFound });
                return;
            }

            var chatRoomGroup = ChatRoomGroupManager.Instance.GetChatRoomGroup(_roomId);

            var(success, errorCode) = await chatRoomGroup.LeaveChatRoomAsync(_roomId, this);
            if (success)
            {
                _roomId = 0;
                _session.SendResponse(PacketCommand.CsLeaveChatRoomSuccess, new CsLeaveChatRoomSuccess());
            }
            else
            {
                _session.SendResponse(PacketCommand.CsLeaveChatRoomFailure, new CsCommonFailure() { errorCode = errorCode });
            }
        }

        public async Task BroadcastChatMessageAysnc(ChatMessage chatMessage)
        {
            //참여 중인 채팅 방이 없는 경우
            if (_roomId == 0)
            {
                Log.Error($"Not in any chat room - Id:{Id}");
                _session.SendResponse(PacketCommand.CsBroadcastChatMessageFailure, new CsCommonFailure() { errorCode = ServerErrorCode.JoinedChatRoomNotFound });
                return;
            }

            var chatRoomGroup = ChatRoomGroupManager.Instance.GetChatRoomGroup(_roomId);

            var (success, errorCode) = await chatRoomGroup.BroadcastMessageAsync(_roomId, chatMessage);
            if (success)
            {
                _session.SendResponse(PacketCommand.CsBroadcastChatMessageSuccess, new CsLeaveChatRoomSuccess());
            }
            else
            {
                _session.SendResponse(PacketCommand.CsBroadcastChatMessageFailure, new CsCommonFailure() { errorCode = errorCode });
            }
        }
    }
}
