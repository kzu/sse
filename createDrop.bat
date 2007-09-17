call sign-assembly.bat Main\Source\bin\Release\SimpleSharing.dll %1
call sign-assembly.bat Main\Source\bin\Release\CF\SimpleSharing.dll %1

xcopy Main\Source\bin\Release\SimpleSharing.* Drops\Current /F /Y /R
xcopy Main\Source\bin\Release\CF\SimpleSharing.* Drops\Current /F /Y /R
