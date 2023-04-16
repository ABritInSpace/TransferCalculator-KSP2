@echo off


taskkill /f /im KSP2_x64.exe
timeout 2


dotnet build InterplanetaryCalc.sln -c Debug
call make_zip.bat Debug
call copy_to_ksp.bat