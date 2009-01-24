msbuild /p:Configuration=Release Main\SimpleSharing.sln

xcopy .\Main\Source\bin\Release\SimpleSharing.* .\Drops\Current\ /F /Y /R
xcopy .\Main\UnitTests\bin\Release\SimpleSharing.Tests.* .\Drops\Current\ /F /Y /R

call %~p0sign-assembly.bat .\Drops\Current\SimpleSharing.dll %1
call %~p0sign-assembly.bat .\Drops\Current\SimpleSharing.Tests.dll %1

rem xcopy .\Main\Source\bin\Release\CF\SimpleSharing.CF.* .\Drops\Current\ /F /Y /R
rem xcopy .\Main\Adapters\SimpleSharing.Data\Source\bin\Release\SimpleSharing.Data.* .\Drops\Current\ /F /Y /R
rem xcopy .\Main\Adapters\SimpleSharing.Data\Source.CF\bin\Release\SimpleSharing.Data.CF.* .\Drops\Current\ /F /Y /R

rem call %~p0sign-assembly.bat .\Drops\Current\SimpleSharing.CF.dll %1
rem call %~p0sign-assembly.bat .\Drops\Current\SimpleSharing.Data.dll %1
rem call %~p0sign-assembly.bat .\Drops\Current\SimpleSharing.Data.CF.dll %1
