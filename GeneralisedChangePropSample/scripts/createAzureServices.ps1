
param (
    [string]$Location = "northeurope",
    [string]$ResourceGroup = "genchangeproprg",
    [string]$ADTInstanceName = "genchangepropadt",
    [string]$ADTDataowner = $(az account show --query user.name -o tsv)
)

// Create resource group
az group create --name $ResourceGroup --location $Location
az configure --defaults group=$ResourceGroup
az configure --defaults location=$Location

// Create ADT instance
az dt create --dt-name $ADTInstanceName -g genchangeproprg
az dt role-assignment create --dt-name $ADTInstanceName --assignee $ADTDataowner --role "Azure Digital Twins Data Owner"

// Upload models
az dt model create --dt-name $ADTInstanceName --from-directory ".\\ontology"

// Create Twins
az dt twin create --dt-name $ADTInstanceName --dtmi "dtmi:sample:genpropchanges:Device;1" --twin-id "TemperatureSensor01" --properties '{\"TemperatureLastValue\" : 0 }'
az dt twin create --dt-name $ADTInstanceName --dtmi "dtmi:sample:genpropchanges:Device;1" --twin-id "TemperatureSensor02" --properties '{\"TemperatureLastValue\" : 0 }'
az dt twin create --dt-name $ADTInstanceName --dtmi "dtmi:sample:genpropchanges:Asset;1" --twin-id "Room01" --properties '{\"Temperature\" : 0 }'
az dt twin relationship create --dt-name $ADTInstanceName --relationship "definedby" --relationship-id "Room01ToTemperatureSensor01" --source "Room01" --target "TemperatureSensor01" --properties "relProps.json"
az dt twin create --dt-name $ADTInstanceName --dtmi "dtmi:sample:genpropchanges:Asset;1" --twin-id "Projector01" --properties '{\"Temperature\" : 0 }'
az dt twin relationship create --dt-name $ADTInstanceName --relationship "contains" --relationship-id "Room01Projector01" --source "Room01" --target "Projector01"
az dt twin relationship create --dt-name $ADTInstanceName --relationship "definedby" --relationship-id "Projector01ToTemperatureSensor01" --source "Projector01" --target "TemperatureSensor01" --properties "relProps.json"

// Create event grid topic for change propagation and install event route
az eventgrid topic create --name $ADTInstanceName -g $ResourceGroup
az dt endpoint create eventgrid --dt-name $ADTInstanceName --eventgrid-resource-group genchangeproprg --eventgrid-topic genchangepropadt --endpoint-name genchangepropeg01
az dt route create --dt-name $ADTInstanceName --endpoint-name genchangepropeg01 --route-name devicePropChanges `
    --filter "(type = 'Microsoft.DigitalTwins.Twin.Update') AND (`$body.modelId = 'dtmi:sample:genpropchanges:Device;1')"

// Create Function App for change propagation
az storage account create --name "genchangepropadtfuncdata" --sku Standard_LRS
az functionapp create --consumption-plan-location northeurope --runtime dotnet --functions-version 3 --name "genchangepropadtfuncs" --storage-account "genchangepropadtfuncdata"
$funcprincId=$(az functionapp identity assign -n genchangepropadtfuncs --query principalId -o tsv)
az dt role-assignment create --dt-name $ADTInstanceName --assignee $funcprincId --role "Azure Digital Twins Data Owner"
$ADTServiceURL = "ADT_SERVICE_URL=https://" + $ADTInstanceName + ".api.neu.digitaltwins.azure.net"
az functionapp config appsettings set -n genchangepropadtfuncs --settings $ADTServiceURL

// Create Event grid topic subscription for Function App
// Filter with --advanced-filter data.modelId StringIn "dtmi:sample:genpropchanges:Device;1" doesnt work, but is not necessary, since event route filtering already active
$SubID=$(az account show --query "id" -o tsv)
az eventgrid event-subscription create -n devicechangehdl `
    --source-resource-id /subscriptions/$SubID/resourceGroups/$ResourceGroup/providers/Microsoft.EventGrid/topics/$ADTInstanceName `
    --endpoint /subscriptions/$SubID/resourceGroups/$ResourceGroup/providers/Microsoft.Web/sites/genchangepropadtfuncs/functions/DeviceChangePropagation --endpoint-type azurefunction
    
