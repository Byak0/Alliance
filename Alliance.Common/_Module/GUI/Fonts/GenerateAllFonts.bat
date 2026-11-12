@echo off
setlocal enabledelayedexpansion
rem Batch-generates font atlases for every .ttf/.otf under this Fonts folder.
rem Mirrors GenerateFont.bat (settings + exe at ..\..\..\bin\Win64_Shipping_wEditor)
rem but passes -fontpath/-outputpath per font instead of the settings placeholders.

set "settingsFile=GenerateFontSettings.txt"
if not exist "%settingsFile%" (
	echo ERROR: %settingsFile% not found in this folder.
	pause
	exit /b 1
)

rem Accumulate settings lines, dropping the per-font -fontpath/-outputpath placeholders
rem and the tool's interactive pause flag if present.
set "baseArgs="
for /f "usebackq delims=" %%a in ("%settingsFile%") do (
	echo %%a | findstr /r /c:"^-fontpath" /c:"^-outputpath" >nul
	if errorlevel 1 (
		set "line=%%a"
		set "baseArgs=!baseArgs! !line!"
	)
)

set "atlasExe=..\..\..\..\bin\Win64_Shipping_wEditor\TaleWorlds.GauntletUI.FontAtlas.exe"
if not exist "%atlasExe%" (
	echo ERROR: TaleWorlds.GauntletUI.FontAtlas.exe not found. Is the Modding Kit installed?
	pause
	exit /b 1
)

set /a total=0
set /a failed=0

for /r "%~dp0" %%f in (*.ttf *.otf) do (
	rem Skip anything inside the tool folder itself
	echo %%f | findstr /i /c:"\GenerateFont" >nul && (
		echo SKIP tool file: %%f
	) || (
		set "fontFile=%%f"
		set "fontName=%%~nf"
		set "fontDir=%%~dpf"
		set "outDir=%~dp0!fontName!\"
		if not exist "!outDir!" mkdir "!outDir!"
		echo ************************************
		echo Building font atlas: !fontName!
		pushd "%~dp0"
		"%atlasExe%" !baseArgs! -fontpath "!fontFile!" -outputpath "!outDir!!fontName!.png"
		if errorlevel 1 (
			set /a failed+=1
			echo ***ERROR*** building !fontName!
		) else (
			echo SUCCESS: !fontName!
		)
		popd
		set /a total+=1
	)
)

echo ************************************
echo Done. %total% processed, %failed% failed.
pause
