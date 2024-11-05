param location string = resourceGroup().location

var prefix = 'weatherapp'
var serverFarmName = '${prefix}sf'
var storageAccountName = '${prefix}sta'

var startJobFunctionName = '${prefix}StartJob'
var processJobFunctionName = '${prefix}ProcessJob'
var generateImageFunctionName = '${prefix}GenerateImage'
var fetchResultsFunctionName = '${prefix}FetchResults'

resource serverFarm 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: serverFarmName
  location: location
  tags: resourceGroup().tags
  sku: {
    tier: 'Consumption'
    name: 'Y1'
  }
  kind: 'elastic'
}

var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: resourceGroup().tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    accessTier: 'Hot'
    publicNetworkAccess: 'Enabled'
  }
}

// StartJob Function
resource startJobFunction 'Microsoft.Web/sites@2021-03-01' = {
  name: startJobFunctionName
  location: location
  tags: resourceGroup().tags
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'functionapp'
  properties: {
    enabled: true
    serverFarmId: serverFarm.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      http20Enabled: true
    }
    clientAffinityEnabled: false
    httpsOnly: true
    containerSize: 1536
    redundancyMode: 'None'
  }
}

resource startJobFunctionConfig 'Microsoft.Web/sites/config@2021-03-01' = {
  name: '${startJobFunctionName}/appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED: '1'
    WEBSITE_RUN_FROM_PACKAGE: '1'
    AzureWebJobsStorage: storageAccountConnectionString
    QueueStorage: storageAccountConnectionString
    TableStorage: storageAccountConnectionString
  }
}

// ProcessJob Function
resource processJobFunction 'Microsoft.Web/sites@2021-03-01' = {
  name: processJobFunctionName
  location: location
  tags: resourceGroup().tags
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'functionapp'
  properties: {
    enabled: true
    serverFarmId: serverFarm.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      http20Enabled: true
    }
    clientAffinityEnabled: false
    httpsOnly: true
    containerSize: 1536
    redundancyMode: 'None'
  }
}

resource processJobFunctionConfig 'Microsoft.Web/sites/config@2021-03-01' = {
  name: '${processJobFunctionName}/appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED: '1'
    WEBSITE_RUN_FROM_PACKAGE: '1'
    AzureWebJobsStorage: storageAccountConnectionString
    QueueStorage: storageAccountConnectionString
    TableStorage: storageAccountConnectionString
  }
}

// GenerateImage Function
resource generateImageFunction 'Microsoft.Web/sites@2021-03-01' = {
  name: generateImageFunctionName
  location: location
  tags: resourceGroup().tags
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'functionapp'
  properties: {
    enabled: true
    serverFarmId: serverFarm.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      http20Enabled: true
    }
    clientAffinityEnabled: false
    httpsOnly: true
    containerSize: 1536
    redundancyMode: 'None'
  }
}

resource generateImageFunctionConfig 'Microsoft.Web/sites/config@2021-03-01' = {
  name: '${generateImageFunctionName}/appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED: '1'
    WEBSITE_RUN_FROM_PACKAGE: '1'
    AzureWebJobsStorage: storageAccountConnectionString
  }
}

// FetchResults Function
resource fetchResultsFunction 'Microsoft.Web/sites@2021-03-01' = {
  name: fetchResultsFunctionName
  location: location
  tags: resourceGroup().tags
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'functionapp'
  properties: {
    enabled: true
    serverFarmId: serverFarm.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      http20Enabled: true
    }
    clientAffinityEnabled: false
    httpsOnly: true
    containerSize: 1536
    redundancyMode: 'None'
  }
}

resource fetchResultsFunctionConfig 'Microsoft.Web/sites/config@2021-03-01' = {
  name: '${fetchResultsFunctionName}/appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
    WEBSITE_RUN_FROM_PACKAGE: '1'
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED: '1'
    AzureWebJobsStorage: storageAccountConnectionString
    AzureWebJobsStorageKey: storageAccount.listKeys().keys[0].value
    AzureAccountName: storageAccount.name
  }
}
