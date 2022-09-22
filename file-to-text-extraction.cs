using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Text;
using System.Threading;
using System.Net.Http;
using System.Web;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Azure.Storage.Blobs;
using Azure.AI.Translation.Document;
using Azure;

namespace cogproject
{
    public class convertfiletotext
    {
        private static readonly string texttranslatorsubscriptionKey = Environment.GetEnvironmentVariable("texttranslatorsubscriptionKey");
        private static readonly string texttranslatorendpoint = Environment.GetEnvironmentVariable("texttranslatorendpoint");
        private static readonly string fileURL = Environment.GetEnvironmentVariable("fileURL");
        private static readonly string StorageContainerString = Environment.GetEnvironmentVariable("StorageContainerString");
        private static readonly string endpoint = Environment.GetEnvironmentVariable("compVisionEndpoint");
        private static readonly string subscriptionKey = Environment.GetEnvironmentVariable("compVisionSubscriptionKey");
        private static readonly string translatefrom = Environment.GetEnvironmentVariable("translatefrom");
        private static readonly string translateto = Environment.GetEnvironmentVariable("translateto");
        private static readonly StringBuilder sb = new StringBuilder();
        

       [FunctionName("convertfiletotext")]
        [StorageAccount("StorageContainerString")]
        public async Task RunAsync([BlobTrigger("sourcefiles/{name}", Connection = "")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            log.LogInformation("fileurl:" + fileURL);
            log.LogInformation("endpoint:" + endpoint);
            log.LogInformation($"Full blob path: {myBlob}");
          


            /* Uri sourceUri = new Uri("https://savdemosa.blob.core.windows.net/source?sp=rcwdli&st=2022-08-30T07:35:25Z&se=2022-10-07T15:35:25Z&spr=https&sv=2021-06-08&sr=c&sig=wAvQBWrzO32X5ymibCze2pZQhQY8LSBVCa%2F8IijVJqg%3D");
             Uri targetUri = new Uri("https://savdemosa.blob.core.windows.net/target?sp=racwdli&st=2022-08-27T11:40:22Z&se=2022-10-08T15:40:22Z&spr=https&sv=2021-06-08&sr=c&sig=or2pm5pOmxDn4GoDnC%2Foff%2FEYGLsBkxBKTb9CTytleQ%3D");
             DocumentTranslationClient dcclient = new DocumentTranslationClient(new Uri(endpoint), new AzureKeyCredential(subscriptionKey));

             DocumentTranslationInput input = new DocumentTranslationInput(sourceUri, targetUri, "hi");
             DocumentTranslationOperation operation = await dcclient.StartTranslationAsync(input);

             await operation.WaitForCompletionAsync();

             Console.WriteLine("  Status: {operation.Status}");
             Console.WriteLine("  Created on: {operation.CreatedOn}");
             Console.WriteLine("  Last modified: {operation.LastModified}");
             Console.WriteLine("  Total documents: {operation.DocumentsTotal}");
             Console.WriteLine("    Succeeded: {operation.DocumentsSucceeded}");
             Console.WriteLine("    Failed: {operation.DocumentsFailed}");
             Console.WriteLine("    In Progress: {operation.DocumentsInProgress}");
             Console.WriteLine("    Not started: {operation.DocumentsNotStarted}");

             await foreach (DocumentStatusResult doc in operation.Value)
             {
                 Console.WriteLine("Document with Id: {document.DocumentId}");
                 Console.WriteLine("  Status:{document.Status}");
                 if (doc.Status == DocumentTranslationStatus.Succeeded)
                 {
                     Console.WriteLine("  Translated Document Uri: {document.TranslatedDocumentUri}");
                     Console.WriteLine("  Translated to language: {document.TranslatedTo}.");
                     Console.WriteLine("  Document source Uri: {document.SourceDocumentUri}");
                 }
                 else
                 {
                     Console.WriteLine("  Error Code: {document.Error.ErrorCode}");
                     Console.WriteLine("  Message: {document.Error.Message}");
                 }
             }


             */
            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);


            Console.WriteLine("----------------------------------------------------------");
            log.LogInformation("READ FILE FROM URL");
            Console.WriteLine();
            // Request parameters
            var clientHTTP = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            // Request headers
            // Request parameters
            queryString["language"] = "en";
            queryString["readingOrder"] = "natural";
            queryString["model-version"] = "latest";
            var uri = endpoint + "vision/v3.2/read/analyze?" + queryString;

            log.LogInformation($"endpoint {uri}...");
            //public static Task<ReadHeaders> ReadAsync(this IComputerVisionClient operations, string url, string language = null, IList<string> pages = null, string modelVersion = "latest", string readingOrder = "natural", CancellationToken cancellationToken = default);
            var textHeaders = await client.ReadAsync(fileURL);
            //   var textHeaders = await client.ReadAsync(fileURL, language: "en", readingOrder: "natural");

            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);
            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);
            log.LogInformation("operationId:" + operationId);
            // Extract the text
            ReadOperationResult results;
            log.LogInformation($"Extracting text from URL file {Path.GetFileName(fileURL)}...");
            Console.WriteLine();

            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
               results.Status == OperationStatusCodes.NotStarted));
            // Display the found text.
            Console.WriteLine();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {

                    // string word = line.Text;
                    string word = line.Text;
                    word = word.Replace(" ", "");
                    word = Regex.Replace(word, "[^a-zA-Z0-9]", String.Empty);
                    bool u = word.All(char.IsUpper);     //returns true
                    bool f = word.All(char.IsLower);

                    if (u)
                    {
                        Console.WriteLine(word);
                        sb.Append(line.Text + Environment.NewLine);
                    }
                    else if (line.Text.EndsWith("."))
                        sb.Append(line.Text + Environment.NewLine);
                    else
                        sb.Append(line.Text + " ");


                }
            }

            log.LogInformation($"2 Extracting text from URL file {sb.ToString()}...");

            translateText(sb.ToString());
            Console.WriteLine("Translated -----------------------------------------------");
            try
            {

                string extension = System.IO.Path.GetExtension(fileURL);
                string parsedfileName = fileURL.Substring(fileURL.LastIndexOf('/') + 1);
                parsedfileName = parsedfileName.Substring(0, parsedfileName.Length - extension.Length);
                parsedfileName = parsedfileName + ".txt";

                string containerName = "processforms";
                // string storageConnection = "DefaultEndpointsProtocol=https;AccountName=solanostorageaccount;AccountKey=DmP9F1nA+IVbDuwhoea3iiFa9rYOo455VmQ5moJiua+CV/OS+5yApGM0+M40OwNlmutqITAOAMVhiiu1FVmaOQ==;EndpointSuffix=core.windows.net";
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageContainerString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
                log.LogInformation("blobContainer::::::" + containerName);

                CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(parsedfileName);
                log.LogInformation("parsedfileName::::::" + parsedfileName);
                await blockBlob.UploadTextAsync(sb.ToString());
                log.LogInformation("blockBlob::::::" + blockBlob);



            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.ToString());
            }

        }

       /* private static async Task translatedocAsync()
        {
            
        string subscriptionKey = "37b737ce05944cc09cb33e4ba41046cf";
            string endpoint = "https://westus2.api.cognitive.microsoft.com/";

            //var client = new DocumentTranslationClient(new Uri(endpoint), new AzureKeyCredential(subscriptionKey));

            Uri sourceUri = new Uri("https://savdemosa.blob.core.windows.net/source?sp=rcwdli&st=2022-08-30T07:35:25Z&se=2022-10-07T15:35:25Z&spr=https&sv=2021-06-08&sr=c&sig=wAvQBWrzO32X5ymibCze2pZQhQY8LSBVCa%2F8IijVJqg%3D");
            Uri targetUri = new Uri("https://savdemosa.blob.core.windows.net/target?sp=racwdli&st=2022-08-27T11:40:22Z&se=2022-10-08T15:40:22Z&spr=https&sv=2021-06-08&sr=c&sig=or2pm5pOmxDn4GoDnC%2Foff%2FEYGLsBkxBKTb9CTytleQ%3D");
            DocumentTranslationClient dcclient = new DocumentTranslationClient(new Uri(endpoint), new AzureKeyCredential(subscriptionKey));

            DocumentTranslationInput input = new DocumentTranslationInput(sourceUri, targetUri, "fil");
            DocumentTranslationOperation operation = await dcclient.StartTranslationAsync(input);

            await operation.WaitForCompletionAsync();

            Console.WriteLine("  Status: {operation.Status}");
            Console.WriteLine("  Created on: {operation.CreatedOn}");
            Console.WriteLine("  Last modified: {operation.LastModified}");
            Console.WriteLine("  Total documents: {operation.DocumentsTotal}");
            Console.WriteLine("    Succeeded: {operation.DocumentsSucceeded}");
            Console.WriteLine("    Failed: {operation.DocumentsFailed}");
            Console.WriteLine("    In Progress: {operation.DocumentsInProgress}");
            Console.WriteLine("    Not started: {operation.DocumentsNotStarted}");

            await foreach (DocumentStatusResult doc in operation.Value)
            {
                Console.WriteLine("Document with Id: {document.DocumentId}");
                Console.WriteLine("  Status:{document.Status}");
                if (doc.Status == DocumentTranslationStatus.Succeeded)
                {
                    Console.WriteLine("  Translated Document Uri: {document.TranslatedDocumentUri}");
                    Console.WriteLine("  Translated to language: {document.TranslatedTo}.");
                    Console.WriteLine("  Document source Uri: {document.SourceDocumentUri}");
                }
                else
                {
                    Console.WriteLine("  Error Code: {document.Error.ErrorCode}");
                    Console.WriteLine("  Message: {document.Error.Message}");
                }
            }
        }
       */
            private static void uploadAudioFileAsync(string filename)
            {
                Console.WriteLine("uploadAudioFileAsync--------------------------------");
                var storageAaccConn = Environment.GetEnvironmentVariable("StorageContainerString");
                // Create a local file in the ./data/ directory for uploading and downloading
                //  string localPath = @"C:\Test\";

                // Write text to the file

                string connectionString = Environment.GetEnvironmentVariable("StorageContainerString");
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

                //Create a unique name for the container
                string containerName = "processedaudio";
                // Create the container and return a container client object
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                // Get a reference to a blob
                BlobClient blobClient = containerClient.GetBlobClient(filename);
                // Console.WriteLine("localFilePath:\n\t {0}\n", localFilePath);
                Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

                // Upload data from the local file
                blobClient.UploadAsync(filename, true);
                Console.WriteLine("Uploaded fil as blob:\n\t {0}\n", filename);


            
        }

        public static async void translateText(string textToTranslate)
        {
            string route = "/translate?api-version=3.0&from=" + translatefrom + "&to=" + translateto;
            StringBuilder sbTrans = new StringBuilder(); //string will be appended later
            //string route = "/translate?api-version=3.0&from=en&to=fil";
            //string textToTranslate = "Hello, world!";
            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(texttranslatorendpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", texttranslatorsubscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "westus2");

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.

                string result = await response.Content.ReadAsStringAsync();
                TranslationResult[] deserializedOutput = JsonConvert.DeserializeObject<TranslationResult[]>(result);
                // Iterate over the deserialized results.
                foreach (TranslationResult o in deserializedOutput)
                {

                    // Iterate over the results and print each translation.
                    foreach (Translation t in o.Translations)
                    {
                        Console.WriteLine("Translated to {0}: {1}", t.To, t.Text);
                        sbTrans = new StringBuilder(t.Text);

                    }
                }



            }

            string extension = System.IO.Path.GetExtension(fileURL);
            string parsedfileName = fileURL.Substring(fileURL.LastIndexOf('/') + 1);
            parsedfileName = parsedfileName.Substring(0, parsedfileName.Length - extension.Length);
            parsedfileName = parsedfileName +"-"+ translateto+ ".txt";

            string containerName = "processforms";
            // string storageConnection = "DefaultEndpointsProtocol=https;AccountName=solanostorageaccount;AccountKey=DmP9F1nA+IVbDuwhoea3iiFa9rYOo455VmQ5moJiua+CV/OS+5yApGM0+M40OwNlmutqITAOAMVhiiu1FVmaOQ==;EndpointSuffix=core.windows.net";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageContainerString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
           

            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(parsedfileName);
            Console.WriteLine("parsedfileNameTRANS::::::" + parsedfileName);
            await blockBlob.UploadTextAsync(sbTrans.ToString());
            Console.WriteLine("blockBlob::::::" + blockBlob);
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        public static async Task ReadFileUrl(ComputerVisionClient client, string urlFile)
        {
            // logMore.LogInformation("ReadFileUrlReadFileUrlReadFileUrlReadFileUrlReadFileUrlReadFileUrl");
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("READ FILE FROM URL");
            Console.WriteLine();
            // Request parameters
            var clientHTTP = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            // Request headers
            // Request parameters
            queryString["language"] = "en";

            queryString["readingOrder"] = "natural";
            queryString["model-version"] = "latest";
            var uri = endpoint + "vision/v3.0/read/analyze?" + queryString;


            //public static Task<ReadHeaders> ReadAsync(this IComputerVisionClient operations, string url, string language = null, IList<string> pages = null, string modelVersion = "latest", string readingOrder = "natural", CancellationToken cancellationToken = default);
            var textHeaders = await client.ReadAsync(urlFile, language: "en", readingOrder: "natural");
            // var textHeaders = await client.ReadAsync(urlFile, "2021-09-30-preview", customers, "natural");
            StringBuilder sb = new StringBuilder();
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);
            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            Console.WriteLine($"Extracting text from URL file {Path.GetFileName(urlFile)}...");
            Console.WriteLine();
            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));
            // Display the found text.
            Console.WriteLine();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {

                    // string word = line.Text;
                    string word = line.Text;
                    word = word.Replace(" ", "");
                    word = Regex.Replace(word, "[^a-zA-Z0-9]", String.Empty);
                    bool u = word.All(char.IsUpper);     //returns true
                    bool f = word.All(char.IsLower);

                    if (u)
                    {
                        Console.WriteLine(word);
                        sb.Append(line.Text + Environment.NewLine);
                    }
                    else if (line.Text.EndsWith("."))
                        sb.Append(line.Text + Environment.NewLine);
                    else
                        sb.Append(line.Text + " ");


                }
            }

            string fileName = @"C:\Temp\extractedfile.txt";

            Uri fileURL = new Uri(urlFile);
            if (fileURL.IsFile)
            {
                string filename = System.IO.Path.GetFileName(fileURL.AbsolutePath);
            }
            try
            {
                // Check if file already exists. If yes, delete it.     
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                string extension = System.IO.Path.GetExtension(urlFile);
                string parsedfileName = urlFile.Substring(urlFile.LastIndexOf('/') + 1);
                parsedfileName = parsedfileName.Substring(0, parsedfileName.Length - extension.Length);
                parsedfileName = parsedfileName + ".txt";
                // Create a new file     
                using (FileStream fs = File.Create(fileName))
                {
                    // Add some text to file    
                    Byte[] title = new UTF8Encoding(true).GetBytes(sb.ToString());
                    fs.Write(title, 0, title.Length);
                    // byte[] author = new UTF8Encoding(true).GetBytes("Mahesh Chand");
                    //fs.Write(author, 0, author.Length);
                }
                string containerName = "processforms";
                // string storageConnection = "DefaultEndpointsProtocol=https;AccountName=solanostorageaccount;AccountKey=DmP9F1nA+IVbDuwhoea3iiFa9rYOo455VmQ5moJiua+CV/OS+5yApGM0+M40OwNlmutqITAOAMVhiiu1FVmaOQ==;EndpointSuffix=core.windows.net";
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageContainerString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
                CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(parsedfileName);
                await blockBlob.UploadTextAsync(sb.ToString());


                // Open the stream and read it back.    
                using (StreamReader sr = File.OpenText(fileName))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        Console.WriteLine(s);
                    }
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.ToString());
            }

        }

    }
}
