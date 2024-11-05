# WeatherImageGenerator

## Functions:
1. HTTPTrigger Function - StartJob
- Accepts an HTTP request (POST) to start a new job.
- Generates a unique jobId.
- Inserts a new entry in Table Storage with jobId, status ("In Progress"), startTime
- Places a message in StartJobQueue with the jobId.
- Returns the jobId

2. QueueTrigger Function - ProcessJob
- Triggers on messages in StartJobQueue.
- Fetches weather data from Buienradar API.
- For each weather station, creates a message in GenerateImageQueue with the weather station data and jobId.
- Updates Table Storage entry for jobId to reflect progress, e.g., marking "Stations Retrieved" or a counter indicating the number of stations queued.

3. QueueTrigger Function - GenerateImage
- Triggers from GenerateImageQueue messages.
- Retrieves a background image from Unsplash.
- Adds weather data text to the image and saves it to Blob Storage with a unique name:`jobId/stationId.jpg`.
- Updates Table Storage entry for jobId to reflect progress, number of images generated, etc.
- Once all images for the job are complete, it updates status to "Completed" in Table Storage.

4. HTTPTrigger Function - FetchResults
- Accepts an HTTP request (GET) with a jobId.
- Fetches all images generated for the jobId from Blob Storage.


## Data
The images are all stored in Blob Storage. The status of the processing is stored in Table Storage. Communication between the functions is done through Azure Queues. There are two queues: StartJobQueue and GenerateImageQueue.

## How to deploy:
Run the following command in PowerShell:

With the following parameters:
- resourceGroup: The name of the resource group to deploy the resources to.
- location: The Azure region to deploy the resources to.
```powershell
./deploy.ps1 -resourceGroup <Resource Group Name> -location <Azure Region (Make sure this is the same as the RG location)>
```
