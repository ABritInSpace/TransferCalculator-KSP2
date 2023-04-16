@REM Create the zip for SpaceDocks
echo off
@REM echo on
set build_mode=%1

@REM define the default build mode to Debug
IF [%build_mode%] == [] set build_mode=Debug

set PROJECT_NAME=InterplanetaryCalc

set OUTPUT=output
set LOCAL_DIR=%OUTPUT%\BepInEx\plugins\%PROJECT_NAME%
set ZIP_File=%PROJECT_NAME%.zip

echo ####################### make zip #######################


@REM create local dir
if not exist %OUTPUT% mkdir %OUTPUT%
if not exist %OUTPUT%\BepInEx mkdir %OUTPUT%\BepInEx
if not exist %OUTPUT%\BepInEx\plugins mkdir %OUTPUT%\BepInEx\plugins

rd /s/q %LOCAL_DIR%

if not exist %LOCAL_DIR% mkdir %LOCAL_DIR%

copy /Y LICENSE.md %LOCAL_DIR%\
copy /Y swinfo.json %LOCAL_DIR%\
copy /Y README.md %LOCAL_DIR%\

md %LOCAL_DIR%\assets
md %LOCAL_DIR%\assets\images
copy /Y icon.png %LOCAL_DIR%\assets\images

set DLL_PATH=InterplanetaryCalc\obj\%build_mode%\netstandard2.0

@REM Copy Dll
copy /Y %DLL_PATH%\%PROJECT_NAME%.dll %LOCAL_DIR%
@REM Copy Pdb
copy /Y %DLL_PATH%\%PROJECT_NAME%.pdb %LOCAL_DIR%

set CWD=%cd%

cd %OUTPUT%

del %ZIP_File%
"C:\Program Files\7-Zip\7z.exe" a %ZIP_File% BepInEx

cd %CWD%


:end