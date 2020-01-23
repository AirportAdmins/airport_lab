set netcoreappversion=%1

if "%1"=="" (
	set netcoreappversion="3.0"
)

rem Specify components you want to start
for /F %%x in (components.txt) do (
	if not exist .\AirportProject\%%x\bin\Debug\netcoreapp%netcoreappversion%\%%x.dll echo "ERROR_ERROR_ERROR"
)