xcopy .\Main\Source\bin\Release\SimpleSharing.* .\Drops\Current\ /F /Y /R
xcopy .\Main\Source\bin\Release\CF\SimpleSharing.CF.* .\Drops\Current\CF\ /F /Y /R
xcopy .\Main\UnitTests\bin\Release\SimpleSharing.Tests.* .\Drops\Current\ /F /Y /R

call %~p0sign-assembly.bat .\Drops\Current\SimpleSharing.dll %1
call %~p0sign-assembly.bat .\Drops\Current\CF\SimpleSharing.CF.dll %1
call %~p0sign-assembly.bat .\Drops\Current\SimpleSharing.Tests.dll %1
