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
- Fetches the status of the job from Table Storage.
- If the job is completed, returns a list of URLs for the generated images.


## How to deploy:
```bash
az group create --name DevOpsAssignmentGroup --location westeurope
```

```bash
az deployment group create \
  --resource-group DevOpsAssignmentGroup \
  --template-file main.bicep \
  --parameters functionAppName="WeatherImageFunctionApp" \
              storageAccountName="weatherimagestorageacct" \
              appServicePlanName="WeatherImageAppServicePlan"
```


## TODO:
- [ ] Check the requirements
- [ ] Deploy start job
- [ ] Deploy process job
- [ ] Deploy generate image
- [ ] Deploy fetch results



