# Define variables
$resourceGroupName = "DevOpsAssignmentGroup"
$location = "westeurope"
$templateFile = "./main.bicep"
$functionAppName = "WeatherImageFunctionApp"
$storageAccountName = "weatherimagestorageacct"
$appServicePlanName = "WeatherImageAppServicePlan"
$projects = @(
    "../WeatherImageGenerator/WeatherImageGenerator.StartJob"
    # "../WeatherImageGenerator/WeatherImageGenerator.ProcessJob",
    # "../WeatherImageGenerator/WeatherImageGenerator.GenerateImage",
    # "../WeatherImageGenerator/WeatherImageGenerator.FetchResults"
)

# Creat the infrastructure using the main.bipec file
Write-Output "Creating the infrastructure..."
az deployment group create `
  --resource-group $resourceGroupName `
  --template-file $templateFile `
  --location $location `
  --parameters `
    functionAppName=$functionAppName `
    storageAccountName=$storageAccountName `
    appServicePlanName=$appServicePlanName

# Loop through each project to publish and deploy to Azure
foreach ($project in $projects) {
    # Define the publish output directory
    $publishFolder = "$project/bin/Release/net8.0/publish"

    # Publish the project
    Write-Output "Publishing project $project..."
    dotnet publish $project -c Release -o $publishFolder

    # Define the path for the zip file
    $zipPath = "$project/publish.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath }

    # Zip the contents of the publish folder (not the folder itself)
    Write-Output "Zipping the contents of the publish folder..."
    Compress-Archive -Path "$publishFolder/*" -DestinationPath $zipPath

    # Deploy to the Azure Function App
    Write-Output "Deploying $project to Azure..."
    az functionapp deployment source config-zip `
      --resource-group $resourceGroupName `
      --name $functionAppName `
      --src $zipPath 

    Write-Output "Deployment of $project completed!"
}

Write-Output "Deployment completed!"
