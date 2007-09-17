@echo Usage: sign MyAssembly[dll or exe]

@echo off
sn -Ra %1 %~p0\Mvp-Xml.Net20.snk
%~p0\signcode -spc %~p0\xmlmvpcert.spc -v %~p0\xmlmvpkey.pvk -t http://timestamp.verisign.com/scripts/timstamp.dll %1