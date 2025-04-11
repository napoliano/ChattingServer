
# 고성능 비동기 채팅 서버 구현
### 특징
#### 
- EAP 기반의 네트워크 코어
- Protobuf 사용(protobuf-net)
- POH 영역 및 Object pool 활용
- 순차적 이벤트 처리를 위한 Channel 활용
- 
---

### 2025-04-11
- ChatRoom을 grouping한 ChatRoomGroup 구현
- ChatRoomGroup을 관리하는 ChatRoomGroupManager 구현
- 채팅 방 관련 기본 기능 구현
- 채팅 참여자 interface 정의 및 User 클래스 일부 구현

### 2025-04-10
- 최초 커밋
- 네트워크 코어 로직 구현
