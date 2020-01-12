#!/bin/sh

netcoreappversion=${1:-3.0}

cat components.txt | while read -r component ; do osascript -e 'tell app "Terminal" to do script "cd '$(pwd)' ; dotnet ./AirportProject/'$component'/bin/Debug/netcoreapp'$netcoreappversion'/'$component'.dll"' ; sleep 0.2 ; done
