
param (
    [string]$Location = "northeurope",
    [string]$ResourceGroup = "genchangeproprg",
    [string]$ADTInstanceName = "genchangepropadt"
)

az configure --defaults group=$ResourceGroup
az configure --defaults location=$Location

az dt twin relationship delete --dt-name $ADTInstanceName --relationship-id "Room01ToTemperatureSensor01" --source "Room01"
az dt twin relationship delete --dt-name $ADTInstanceName --relationship-id "Room01Projector01" --source "Room01"
az dt twin relationship delete --dt-name $ADTInstanceName --relationship-id "Projector01ToTemperatureSensor01" --source "Projector01"
az dt twin delete --dt-name $ADTInstanceName --twin-id "Room01"
az dt twin delete --dt-name $ADTInstanceName --twin-id "Projector01"
az dt twin delete --dt-name $ADTInstanceName --twin-id "TemperatureSensor01"

az dt model delete --dt-name $ADTInstanceName --dtmi "dtmi:sample:genpropchanges:Device;1"
az dt model delete --dt-name $ADTInstanceName --dtmi "dtmi:sample:genpropchanges:Asset;1"