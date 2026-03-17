@echo off
setlocal

for %%P in (3007 3008 3009 3010 3011 3000 5173 5174) do (
  for /f "tokens=5" %%A in ('netstat -ano ^| findstr :%%P ^| findstr LISTENING') do (
    echo Killing PID %%A on port %%P
    taskkill /PID %%A /F >nul 2>nul
  )
)

endlocal
