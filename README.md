# Speech Analytics Demo
This demo highlights the power of combining Microsoft [AI Services Speech to Text](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/) and [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service) to transcribe and analyze the content of a call center conversation.
It will extract the call sentiment, and action items that were discussed during the call and also summarize problem statements and root causes


## Get Started

To simplify the deployment of the demo, we have created Azure Bicep templates and a PowerShell script to deploy the required resources to your Azure subscription. Becuase Azure Open AI resources are not yet available in all regions, you will need to [deploy that on your own](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal) and provide the endpoint and key to the deployment script.

Then, simply login to the Azure CLI and run the deployment script:

``` PowerShell
az login
.\deploy.ps1 -resourceGroup "<rg name>"-location "<azure region>" -aiServicesAcctName "<ai svc name>" -storageAcctName "<storage acct>" -azureOpenAiEndpoint "<exising AOAI endpoint>" -azureOpenAiKey "<existing AOAI key>"
```



## How to use

When you run the app for the first time, select the `1` option to analyze a new call audio file.\
You will be prompted to enter the path to the audio file. The app will then upload the file to Blob storage and start the analysis process.

<img src="images/first_run.png" width="40%">

The app will:
1. Uplaod the audio file to Blob storage
2. Start the Speech to Text transcription batch process
3. Poll the transcription process until it is complete
4. Save the transcription results to Blob storage
5. Start the Azure Open AI sentiment analusis process
6. Run the Azure Open AI action item extraction process
7. Run the Azure Open AI problem statement and root cause determination process

As each step is complete, the app will display the results in the console.

Upon running again, the app will locate any prior transcription results and prompt you to use those or upload a new file.

<img src="images/subsequent_run.png" width="40%">


