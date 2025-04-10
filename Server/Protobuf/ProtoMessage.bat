
set FILE_DIR=C:\repos\Server\Server\Protobuf

%USERPROFILE%\.dotnet\tools\protogen.exe ProtoMessage.proto --proto_path=%FILE_DIR% --csharp_out=%FILE_DIR% +names=original

pause
