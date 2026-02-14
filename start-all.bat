@echo off
setlocal

set ROOT=%~dp0
pushd "%ROOT%"

echo Building frontend...
pushd "frontend"
call npm run build
popd

echo Starting backend...
start "SimplerJiangAiAgent.Api" cmd /k "dotnet run --project backend\SimplerJiangAiAgent.Api\SimplerJiangAiAgent.Api.csproj"

timeout /t 3 >nul

echo Starting desktop...
start "SimplerJiangAiAgent.Desktop" cmd /k "dotnet run --project desktop\SimplerJiangAiAgent.Desktop\SimplerJiangAiAgent.Desktop.csproj"

popd
endlocal
