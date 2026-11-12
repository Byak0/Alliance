@echo off
set "settingsFile=GenerateFontSettings.txt"
if not exist %settingsFile% (
	echo ************************************
	echo ERROR reading the settings file
	echo Please see the document for more info
	echo Press any key to exit...
	pause> nul
	exit /b
)

for /f "delims=" %%a in ('Type "%settingsFile%"') do (
	echo %%a
    call set fontAtlasArguments=%%fontAtlasArguments%% %%a
)

cd ..\..\..\..\bin\Win64_Shipping_wEditor

if not exist TaleWorlds.GauntletUI.FontAtlas.exe (
	echo ************************************
	echo ERROR!
	echo bin\Win64_Shipping_wEditor\TaleWorlds.GauntletUI.FontAtlas could not be found
	echo Please make sure you have Modding Kit installed.
	echo See the document for more info.
	echo ************************************
	pause>nul|(echo Press any key to exit...)
	exit
)

echo ************************************
echo Building font atlas : %fontAtlasArguments%
echo ************************************
start /wait TaleWorlds.GauntletUI.FontAtlas.exe %fontAtlasArguments%
set "atlasGeneratorErr=%ERRORLEVEL%"
if %atlasGeneratorErr%==0 (echo SUCCESS...) else (pause>nul|(echo ************************************ && echo ***ERROR*** building the font atlas. Error Code: %atlasGeneratorErr% && echo Please see the document for more info && echo Press any key to exit...))
if NOT %atlasGeneratorErr%==0 exit /b
pause>nul|(echo Press any key to exit...)