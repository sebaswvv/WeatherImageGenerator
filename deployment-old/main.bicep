param functionAppName string
param location string = resourceGroup().location
param storageAccountName string
param appServicePlanName string
param functionAppRuntimeVersion string = '4'
param skuName string = 'Y1'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource functionAppPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    tier: 'Dynamic'
    name: skuName
  }
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionAppPlan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccount.properties.primaryEndpoints.blob
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: functionAppRuntimeVersion
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
      ]
    }
  }
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  name: '${storageAccount.name}/default/weatherimages'
  properties: {
    publicAccess: 'None'
  }
}

resource table 'Microsoft.Storage/storageAccounts/tableServices/tables@2022-09-01' = {
  name: '${storageAccount.name}/default/WeatherImageGeneratorJobs'
}

resource queue 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-09-01' = {
  name: '${storageAccount.name}/default/generateimagequeue'
}

output functionAppUrl string = functionApp.properties.defaultHostName
output storageAccountConnectionString string = storageAccount.properties.primaryEndpoints.blob
