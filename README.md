
# 고성능 비동기 채팅 서버 구현
### 소개
#### 
고성능 비동기 채팅 서버 구현을 목표로 작성한 포트폴리오입니다.  
계속 업데이트 중이며, 구현이 불필요하다고 느끼는 부분은 임시 코드만 작성돼 있을 수 있습니다.

### 특징
#### 
- EAP 기반의 네트워크 코어
- TAP 기반의 로직 처리
- Protobuf 사용(protobuf-net)
- POH 영역 및 Object pool 활용
- 순차적 처리를 위한 Channel 활용
- Frozen container 사용
- 
---
### 2025-04-12
 - 패킷 핸들 및 채팅 로직 비동기 처리

### 2025-04-11
- ChatRoom을 grouping한 ChatRoomGroup 구현
- ChatRoomGroup을 관리하는 ChatRoomGroupManager 구현
- 채팅 방 관련 기본 기능 구현
- 채팅 참여자 interface 정의 및 User 클래스 일부 구현

### 2025-04-10
- 최초 커밋
- 네트워크 코어 로직 구현
