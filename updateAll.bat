rem Specify components you want to start
for /F %%x in (branches.txt) do (
	git pull origin %%x
)