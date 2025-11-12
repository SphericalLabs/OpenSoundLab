@echo off
set clients=3
for /l %%x in (0, 1, %clients%-1) do (
    start "" "..\..\..\..\Builds\OML\OML - Windows\OpenMultiLab.exe" -ip 192.168.0.164 -port 7777 -autoconnect
)

:: -ip: Defaults to localhost if not given.
:: -port: Defaults to Mirror's default port 7777 if not given.
:: -autoconnect: Program will connect automatically when started, if autoconnect is not set than press key 'c' while the app is in focus.
:: -relaycode: If this is present, only Relay will be connected to (not working yet).
