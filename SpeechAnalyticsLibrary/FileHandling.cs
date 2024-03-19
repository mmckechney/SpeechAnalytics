namespace SpeechAnalyticsLibrary
{
   using Azure;
   using Azure.Storage.Blobs;
   using Microsoft.Extensions.Logging;
   using SpeechAnalyticsLibrary;

   public class FileHandling
   {
      private ILogger log;
      private IdentityHelper identityHelper;
      public FileHandling(ILogger<FileHandling> log, IdentityHelper identityHelper)
      {
         this.log = log;
         this.identityHelper = identityHelper;
      }

      public async Task<List<string>> GetListOfAudioFilesInContainer(string sourceContainerUrl)
      {
         try
         {
            List<string> files = new();
            BlobContainerClient containerClient = GetContainerClient(sourceContainerUrl);
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
               files.Add(blob.Name);
            }
            return files;
         }
         catch (Exception exe)
         {
            log.LogError($"Error: {exe.Message}");
            return new List<string>();
         }
      }
      public async Task<string> UploadBlobForTranscription(FileInfo file, string sourceContainerUrl)
      {
         try
         {
            BlobContainerClient containerClient = GetContainerClient(sourceContainerUrl);

            //upload the file to blob storage using the sourceURL SAS token
            var blobClient = containerClient.GetBlobClient(file.Name);
            var result = await blobClient.UploadAsync(file.FullName, true);

            //validate that the upload was successful
            if (result.GetRawResponse().Status == 201)
            {
               log.LogInformation("File uploaded successfully");
               // DeletePreExistingFile(file, targetSasUrl);
               return blobClient.Uri.AbsoluteUri.ToString();
            }
            else
            {
               log.LogError("File upload failed");
               return string.Empty;
            }

            //Delete any pre-translated document

         }
         catch (Exception exe)
         {
            log.LogError($"Error: {exe.Message}");
            return string.Empty;
         }
      }

      public async Task<string> SaveTranscriptionFile(string sourceFileName, string transcriptionText, string targetContainerUrl)
      {
         try
         {
            BlobContainerClient containerClient = GetContainerClient(targetContainerUrl);

            var fileName = GetTranscriptionFileName(sourceFileName);
            //upload the file to blob storage using the sourceURL SAS token
            var blobClient = containerClient.GetBlobClient(fileName);
            using (MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(transcriptionText)))
            {
               var result = await blobClient.UploadAsync(stream, true);

               if (result.GetRawResponse().Status == 201)
               {
                  log.LogInformation($"File {fileName} uploaded successfully");
                  // DeletePreExistingFile(file, targetSasUrl);
                  return blobClient.Uri.AbsoluteUri.ToString();
               }
               else
               {
                  log.LogError("File upload failed");
                  return string.Empty;
               }
            }
         }
         catch (Exception exe)
         {
            log.LogError($"Error: {exe.Message}");
            return string.Empty;
         }
      }

      public async Task<Dictionary<int, string>> GetTranscriptionList(string containerUrl, int startIndex)
      {
         try
         {
            Dictionary<int, string> files = new();
            int counter = startIndex;
            var containerClient = GetContainerClient(containerUrl);

            var iterator = containerClient.GetBlobsAsync().GetAsyncEnumerator();
            while (await iterator.MoveNextAsync())
            {
               if (!iterator.Current.Name.StartsWith("recording"))
               {
                  files.Add(counter, iterator.Current.Name);
                  counter++;
               }
            }
            return files;
         }
         catch (Exception exe)
         {
            log.LogError($"Error: {exe.Message}");
            return new Dictionary<int, string>();
         }
      }

      public async Task<(string source, string transcription)> GetTranscriptionFileTextFromBlob(string filename, string containerUrl)
      {
         try
         {
            var containerClient = GetContainerClient(containerUrl);
            var blobClient = containerClient.GetBlobClient(filename);
            using (var memory = new MemoryStream())
            {
               await blobClient.DownloadToAsync(memory);
               string content = System.Text.Encoding.UTF8.GetString(memory.ToArray());
               return (filename, content);
            }
         }
         catch (Exception exe)
         {
            log.LogError($"Error: {exe.Message}");
            return ("", "");
         }

      }
      public async Task<bool> DownloadTranscriptionDocument(string path, string fileName, string containerUrl)
      {
         try
         {

            var containerClient = GetContainerClient(containerUrl);
            var blobClient = containerClient.GetBlobClient(fileName);
            string localFile = Path.Combine(path, fileName);
            await blobClient.DownloadToAsync(localFile);
            log.LogInformation($"Translated document saved to:\t {localFile}");
            return true;
         }
         catch (Exception exe)
         {
            log.LogError($"Error: {exe.Message}");
            return false;
         }

      }

      private BlobContainerClient GetContainerClient(string blobContainerUrl)
      {
         var containerUri = new Uri(blobContainerUrl);
         var containerUrl = new Uri($"{containerUri.Scheme}://{containerUri.Host}{containerUri.AbsolutePath}");
         var signature = containerUri.Query?.Length > 0 ? $"{containerUri.Query?.Substring(1)}" : "";

         BlobContainerClient containerClient;
         if (string.IsNullOrWhiteSpace(signature))
         {
            containerClient = new BlobContainerClient(containerUrl, identityHelper.TokenCredential);
         }
         else
         {
            containerClient = new BlobContainerClient(containerUrl, new AzureSasCredential(signature));
         }

         return containerClient;
      }
      private void DeletePreExistingFile(FileInfo file, string blobContainerUrl)
      {
         try
         {

            var containerClient = GetContainerClient(blobContainerUrl);
            var blobClient = containerClient.GetBlobClient(file.Name);
            blobClient.DeleteIfExists();
         }
         catch (Exception exe)
         {
            Console.WriteLine($"Error: {exe.Message}");
         }
      }

      public string GetTranscriptionFileName(FileInfo localSourceFile)
      {
         var name = Path.GetFileNameWithoutExtension(localSourceFile.Name);
         return $"{name}.txt";
      }

      public string GetTranscriptionFileName(string localSourceFile)
      {
         var name = Path.GetFileNameWithoutExtension(localSourceFile);
         return $"{name}.txt";
      }
   }
}
