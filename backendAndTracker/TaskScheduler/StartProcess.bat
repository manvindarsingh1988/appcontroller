@ECHO OFF

SET EXEName=AppController.exe
SET EXEFullPath=$Path$

TASKLIST | FINDSTR /I "%EXEName%"
IF ERRORLEVEL 1 GOTO :StartApp
GOTO :EOF

:StartApp
START "" "%EXEFullPath%"
GOTO :EOF