using System;
using System.IO;
using Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Azure.AI.Translation.Document;
using System.Threading.Tasks;

namespace cogproject
{
    public class transdoc
    {
        [FunctionName("transdoc")]
        [StorageAccount("StorageContainerString")] 
        public async Task RunAsync([BlobTrigger("sourcefiles/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            string subscriptionKey = "**b737ce05944cc09cb33e4ba410***";
            string endpoint = "https://westus2.api.cognitive.microsoft.com/";

            //var client = new DocumentTranslationClient(new Uri(endpoint), new AzureKeyCredential(subscriptionKey));

            Uri sourceUri = new Uri("https://storageaccountname.blob.core.windows.net/sourcefiles?sp=racwdli&st=2022-09-12T22:53:40Z&se=2023-02-18T07:53:40Z&spr=https&sv=2021-06-08&sr=c&sig=b1RJEAOyxVw%2FLAT%2BILUll1oWk8iZIyeoXS7wUmTK%2Fl0%3D");
            Uri targetUri = new Uri("https://storageaccountname.blob.core.windows.net/translatedpdf?sp=racwdli&st=2022-09-20T22:59:30Z&se=2023-03-11T07:59:30Z&spr=https&sv=2021-06-08&sr=c&sig=Iy0L2epVkhBQujVaDp%2BdAXTe251SN4bvkFVMaWYdLgE%3D");
            DocumentTranslationClient dcclient = new DocumentTranslationClient(new Uri(endpoint), new AzureKeyCredential(subscriptionKey));

            DocumentTranslationInput input = new DocumentTranslationInput(sourceUri, targetUri, "es");
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
    }
}
