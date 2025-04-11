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

        public void CreateChatRoom(string title)
        {
            if (_roomId != 0)
            {
                _session.SendResponse(PacketCommand.CsCreateChatRoomFailure, new CsCommonFailure() { errorCode = ServerErrorCode.AlreadyInChatRoom });
                return;
            }

            int roomId = ChatRoomGroupManager.Instance.GetNewRoomId();
            var chatRoomGroup = ChatRoomGroupManager.Instance.GetChatRoomGroup(roomId);

            chatRoomGroup.CreateChatRoom(roomId, title, (success, errorCode) =>
            {
                if (success)
                {
                    _roomId = roomId;
                    _session.SendResponse(PacketCommand.CsCreateChatRoomSuccess, new CsCreateChatRoomSuccess());
                }
                else
                {
                    _session.SendResponse(PacketCommand.CsCreateChatRoomFailure, new CsCommonFailure() { errorCode = errorCode });
                }
            });
        }

        public void JoinChatRoom(int roomId)
        {
            if (_roomId != 0)
            {
                _session.SendResponse(PacketCommand.CsJoinChatRoomFailure, new CsCommonFailure() { errorCode = ServerErrorCode.AlreadyInChatRoom });
                return;
            }

            var chatRoomGroup = ChatRoomGroupManager.Instance.GetChatRoomGroup(roomId);
            chatRoomGroup.JoinChatRoom(roomId, this, (success, errorCode) =>
            {
                if (success)
                {
                    _roomId = roomId;
                    _session.SendResponse(PacketCommand.CsJoinChatRoomSuccess, new CsJoinChatRoomSuccess());
                }
                else
                {
                    _session.SendResponse(PacketCommand.CsJoinChatRoomFailure, new CsCommonFailure() { errorCode = errorCode });
                }
            });
        }

        public void LeaveChatRoom()
        {
            if (_roomId == 0)
            {
                _session.SendResponse(PacketCommand.CsLeaveChatRoomFailure, new CsCommonFailure() { errorCode = ServerErrorCode.JoinedChatRoomNotFound });
                return;
            }

            var chatRoomGroup = ChatRoomGroupManager.Instance.GetChatRoomGroup(_roomId);
            chatRoomGroup.LeaveChatRoom(_roomId, this, (success, errorCode) =>
            {
                if (success)
                {
                    _roomId = 0;
                    _session.SendResponse(PacketCommand.CsLeaveChatRoomSuccess, new CsLeaveChatRoomSuccess());
                }
                else
                {
                    _session.SendResponse(PacketCommand.CsLeaveChatRoomFailure, new CsCommonFailure() { errorCode = errorCode });
                }
            });
        }
    }
}
