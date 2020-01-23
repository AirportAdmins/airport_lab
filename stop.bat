rem Specify components you want to kill
for /F %%x in (components.txt) do (
	taskkill /F /IM %%x.exe
)

taskkill /F /IM VsDebugConsole.exe