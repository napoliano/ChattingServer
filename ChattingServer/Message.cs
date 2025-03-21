using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Akka.Actor;
using UserPacket;


namespace ChattingServer
{
    public record StartServer(int Port);
    public record StopServer();
    public record ProcessAccept(SocketAsyncEventArgs EventArgs);

    public record AddUserSocket(Socket Socket);
    public record RemoveUserSocket(int Id);

    public record StartReceive();
    public record ProcessReceive(SocketAsyncEventArgs EventArgs);
    public record ProcessSend(SocketAsyncEventArgs EventArgs);
    public record SendResponse(byte[] Buffer, int PacketSize);


    #region 채팅방 생성
    public record CreateChatRoomRequest(string Title, int UserLimit);
    public record CreateChatRoom(string Title, int UserLimit);
    public record CreateChatRoomSuccess(int RoomId, IActorRef Room);
    public record CreateChatRoomFailure(ServerErrrorCode ErrorCode);
    #endregion


    #region 채팅방 제거
    public record DestroyChatRoom(int RoomId);
    #endregion


    #region 채팅방 조회(찾기)
    public record FindChatRoom(int RoomId);
    public record FindChatRoomSuccess(IActorRef room);
    public record FindChatRoomFailure(ServerErrrorCode ErrorCode);
    #endregion


    #region 채팅방 입장
    public record JoinChatRoomRequest(int RoomId, bool AutoJoin = false);
    public record JoinChatRoom(int UserId, string UserName, bool AutoJoin = false);
    public record JoinChatRoomSuceessNotify();
    public record JoinChatRoomSuccess(ChatRoomInfo ChatRoomInfo, bool AutoJoin = false);
    public record JoinChatRoomFailure(ServerErrrorCode ErrorCode);
    #endregion


    #region 채팅방 퇴장
    public record LeaveChatRoomRequest();
    public record LeaveChatRoom(int UserId, IActorRef User);
    public record LeaveChatRoomSuccess();
    public record LeaveChatRoomFailure(ServerErrrorCode ErrorCode);
    #endregion


    #region 채팅 메시지
    public record SendChatMessageReqeust(ChatMessage ChatMessage);
    public record SendChatMessageSuccess();
    public record SendChatMessageFailure(ServerErrrorCode ErrorCode);

    public record BroadcastChatMessage(ChatMessage ChatMessage);
    public record SendChatMessageToUser(ChatMessage ChatMessage);
    #endregion
}
