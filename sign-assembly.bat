@echo Usage: sign MyAssembly[dll or exe]

@echo off
sn -Ra %1 %~p0\Mvp-Xml.Net20.snk

echo %~dp0signcode -spc %~dp0xmlmvpcert.spc -v %~dp0xmlmvpkey.pvk -t http://timestamp.verisign.com/scripts/timstamp.dll %~f1
%~dp0signcode -spc %~dp0xmlmvpcert.spc -v %~dp0xmlmvpkey.pvk -t http://timestamp.verisign.com/scripts/timstamp.dll %~f1