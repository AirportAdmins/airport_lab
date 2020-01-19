set netcoreappversion=%1

if "%1"=="" (
	set netcoreappversion="3.0"
)

rem Specify components you want to start
for /F %%x in (components.txt) do (
	start cmd /k dotnet .\AirportProject\%%x\bin\Debug\netcoreapp%netcoreappversion%\%%x.dll
	sleep 0.3
)