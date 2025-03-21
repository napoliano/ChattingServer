using System;
using System.Web;
using System.Runtime.InteropServices;
using Akka.Actor;
using Akka.Event;
using Google.Protobuf;
using UserPacket;


namespace ChattingServer
{
    public class UserActor : ReceiveActor
    {
        private readonly int _uid;
        private readonly string _userName;

        private readonly IActorRef _userSocket;

        private static IActorRef _chatRoomManager;
        
        private IActorRef? _chatRoom;


        public static Props Props(int uid, IActorRef userSocket) => Akka.Actor.Props.Create(() => new UserActor(uid, userSocket));

        public UserActor(int uid, IActorRef userSocket)
        {
            _uid = uid;
            _userName = $"guest_{_uid}";

            _userSocket = userSocket;


            #region 채팅 방 생성
            Receive<CreateChatRoomRequest>(message =>
            {
                //이미 참가 중인 채팅 방이 있는 경우
                if (_chatRoom != null)
                {
                    SendResponse(PacketCommand.CreateChatRoomFailure, new CsCommonFailurePacket() { ErrorCode = ServerErrrorCode.ChatRoomAlreadyJoined });
                    return;
                }

                _chatRoomManager.Tell(new CreateChatRoom(message.Title, message.UserLimit));
            });

            Receive<CreateChatRoomSuccess>(message =>
            {
                _chatRoom = message.Room;

                //채팅 방 생성 시 자동 참가
                _chatRoom.Tell(new JoinChatRoom(_uid, _userName, true));

                SendResponse(PacketCommand.CreateChatRoomSuccess, new CsCreateChatRoomSuccess());
            });

            Receive<CreateChatRoomFailure>(message =>
            {
                SendResponse(PacketCommand.CreateChatRoomFailure, new CsCommonFailurePacket() { ErrorCode = message.ErrorCode });
            });
            #endregion


            #region 채팅 방 입장
            Receive<JoinChatRoomRequest>(message =>
            {
                //이미 참가 중인 채팅 방이 있는 경우
                if (_chatRoom != null)
                {
                    SendResponse(PacketCommand.JoinChatRoomFailure, new CsCommonFailurePacket() { ErrorCode = ServerErrrorCode.ChatRoomAlreadyJoined });
                    return;
                }

                var task = _chatRoomManager.Ask<IActorRef>(new FindChatRoom(message.RoomId));
                task.PipeTo(Self, success: room => new FindChatRoomSuccess(room), failure: ex => new FindChatRoomFailure(ServerErrrorCode.ChatRoomNotFound));
            });

            Receive<FindChatRoomSuccess>(message =>
            {
                _chatRoom = message.room;

                _chatRoom.Tell(new JoinChatRoom(_uid, _userName));
            });

            Receive<FindChatRoomFailure>(message =>
            {
                Self.Tell(new JoinChatRoomFailure(message.ErrorCode));
            });

            Receive<JoinChatRoomSuccess>(message =>
            {
                if (message.AutoJoin == false)
                {
                    SendResponse(PacketCommand.JoinChatRoomSuccess, new CsJoinChatRoomSuccess() { ChatRoomInfo = message.ChatRoomInfo });
                }
                else
                {
                    SendResponse(PacketCommand.JoinChatRoomSuccessNotify, new CsJoinChatRoomSuccessNotify() { ChatRoomInfo = message.ChatRoomInfo });
                }
            });

            Receive<JoinChatRoomFailure>(message =>
            {
                SendResponse(PacketCommand.JoinChatRoomFailure, new CsCommonFailurePacket() { ErrorCode = message.ErrorCode });
            });
            #endregion


            #region 채팅 방 퇴장
            Receive<LeaveChatRoomRequest>(message =>
            {
                //참가 중인 채팅 방이 없는 경우
                if (_chatRoom == null)
                {
                    SendResponse(PacketCommand.LeaveChatRoomFailure, new CsCommonFailurePacket() { ErrorCode = ServerErrrorCode.ChatRoomNotJoined });
                    return;
                }

                _chatRoom.Tell(new LeaveChatRoom(_uid, Self));
            });

            Receive<LeaveChatRoomSuccess>(message =>
            {
                _chatRoom = null;

                SendResponse(PacketCommand.LeaveChatRoomSuccess, new CsLeaveChatRoomSuccess());
            });

            Receive<LeaveChatRoomFailure>(message =>
            {
                SendResponse(PacketCommand.LeaveChatRoomFailure, new CsCommonFailurePacket() { ErrorCode = message.ErrorCode });
            });
            #endregion


            #region 채팅 메시지 처리
            Receive<SendChatMessageReqeust>(message =>
            {
                //참가 중인 채팅 방이 없는 경우
                if (_chatRoom == null)
                {
                    SendResponse(PacketCommand.SendChatMessageFailure, new CsCommonFailurePacket() { ErrorCode = ServerErrrorCode.ChatRoomNotJoined });
                    return;
                }

                message.ChatMessage.SenderName = _userName;
                _chatRoom.Tell(new BroadcastChatMessage(message.ChatMessage));
            });

            Receive<SendChatMessageSuccess>(message =>
            {
                SendResponse(PacketCommand.SendChatMessageSuccess, new CsSendChatMessageSuccess());
            });

            Receive<SendChatMessageFailure>(message =>
            {
                SendResponse(PacketCommand.SendChatMessageFailure, new CsCommonFailurePacket() { ErrorCode = message.ErrorCode });
            });

            Receive<SendChatMessageToUser>(message =>
            {
                SendResponse(PacketCommand.BroadcastChatMessageNotify, new CsBroadcastChatMessageNotify() { ChatMessage = message.ChatMessage });
            });
            #endregion
        }

        public void SendResponse<T>(PacketCommand command, T message) where T : IMessage<T>
        {
            (var buffer, var packetSize) = PacketSerializer.Serialize(command, message);

            _userSocket.Tell(new SendResponse(buffer, packetSize));
        }

        public static void SetChatRoomManager(IActorRef chatRoomManager)
        {
            _chatRoomManager = chatRoomManager;
        }
    }
}