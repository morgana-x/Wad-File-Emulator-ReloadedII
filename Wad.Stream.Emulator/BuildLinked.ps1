# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/Wad.Stream.Emulator/*" -Force -Recurse
dotnet publish "./Wad.Stream.Emulator.csproj" -c Release -o "$env:RELOADEDIIMODS/Wad.Stream.Emulator" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location