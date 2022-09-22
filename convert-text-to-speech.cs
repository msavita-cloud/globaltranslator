using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace cogproject
{



    public class TranslationResult
    {
        public DetectedLanguage DetectedLanguage { get; set; }
        public TextResult SourceText { get; set; }
        public Translation[] Translations { get; set; }
    }
    public class DetectedLanguage
    {
        public string Language { get; set; }
        public float Score { get; set; }
    }

    public class TextResult
    {
        public string Text { get; set; }
        public string Script { get; set; }
    }

    public class Translation
    {
        public string Text { get; set; }
        public TextResult Transliteration { get; set; }
        public string To { get; set; }
        public Alignment Alignment { get; set; }
        public SentenceLength SentLen { get; set; }
    }

    public class Alignment
    {
        public string Proj { get; set; }
    }

    public class SentenceLength
    {
        public int[] SrcSentLen { get; set; }
        public int[] TransSentLen { get; set; }
    }

   
        public class converttexttospeech
        {


            private static readonly string texttranslatorsubscriptionKey = Environment.GetEnvironmentVariable("texttranslatorsubscriptionKey");
            private static readonly string texttranslatorendpoint = Environment.GetEnvironmentVariable("texttranslatorendpoint");
            private static readonly string location = Environment.GetEnvironmentVariable("location");
            private static readonly string speechSubscriptionKey = Environment.GetEnvironmentVariable("speechSubscriptionKey");
            private static readonly string speechEndpoint = Environment.GetEnvironmentVariable("speechEndpoint");
            private static readonly string coglocation = Environment.GetEnvironmentVariable("location");
            private static readonly string lexiconuri = Environment.GetEnvironmentVariable("lexiconuri");
            private static readonly string translatefrom = Environment.GetEnvironmentVariable("translatefrom");
            private static readonly string translateto = "hi-IN";
            private static readonly string StorageContainerString = Environment.GetEnvironmentVariable("StorageContainerString");
            private static string file = "";
        //  private static string drive = @"C:\";
        //  private static string folders = @"Test\";
        private static string Translatedfile = "";
        //  private static string fullPath = Path.Combine(drive, folders, file);

        [FunctionName("converttexttospeech")]
        [StorageAccount("StorageContainerString")]
        public void Run([BlobTrigger("processforms/{name}", Connection = "")] Stream myBlob, string name, ILogger log)
            {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes ");
            log.LogInformation($"StorageContainerString\n StorageContainerString:{StorageContainerString} \n  ");
            var config = SpeechConfig.FromSubscription(speechSubscriptionKey, coglocation);
            StreamReader reader = new StreamReader(myBlob, System.Text.Encoding.UTF8);
            string oldContent = reader.ReadToEnd();


            log.LogInformation($"oldContent:{oldContent}");
            string fileNameToParse = "";


            string[] lines = oldContent.Split(
    new string[] { "\r\n", "\r", "\n" },
    StringSplitOptions.None
);


            fileNameToParse = $"{name}";
            string extension = System.IO.Path.GetExtension(fileNameToParse);
            string parsedfileName = fileNameToParse.Substring(0, fileNameToParse.Length - extension.Length);

            log.LogInformation("Parsing Started for :" + fileNameToParse);

            file = parsedfileName + ".mp3";
            Translatedfile = parsedfileName + "-" + translateto + ".mp3";
            //   fullPath = Path.Combine(drive, folders, file);
            //string fullPathTrans = Path.Combine(drive, folders, Translatedfile);
            //SynthesisSsmlToMp3File(lines, fullPath);// changdeed o n april 3 5.33
            log.LogInformation("lexiconuri:" + lexiconuri);
            SynthesisSsmlToMp3File(lines, file);
            log.LogInformation("SynthesisSsmlToMp3File");
            uploadAudioFileAsync(file);
            log.LogInformation("uploadAudioFileAsync");
            translateText(oldContent, fileNameToParse, Translatedfile);
            Console.WriteLine("FINISHED THE PROCEESS:\n\t {0}\n", fileNameToParse);
        }
        public static async void translateText(string textToTranslate, string fileName, string parsedfileName)
        {
            string route = "/translate?api-version=3.0&from=" + translatefrom + "&to=" + translateto;

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
                        string[] linesFil = t.Text.Split(
    new string[] { "\r\n", "\r", "\n" },
    StringSplitOptions.None
);
                        //  parsedfileName = parsedfileName + "-" + t.To + ".mp3";
                        //  fullPath = Path.Combine(drive, folders, parsedfileName);

                        SynthesisSsmlToMp3File(linesFil, parsedfileName);

                        //   ConvertTextToSpeech(t.Text).Wait();
                        //   parsedfileName = parsedfileName + "-" + t.To + ".mp3";
                        // fullPath = Path.Combine(drive, folders, parsedfileName);

                        //    ConvertTextToAudioFile(t.Text, fullPath).Wait();

                        Console.WriteLine("mp3 FILE------" + t.To);
                        uploadAudioFileAsync(parsedfileName);
                    }
                }



            }

            Console.WriteLine("FINISH:\n\t {0}\n", parsedfileName);
        }

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
            Console.WriteLine("Uploaded file as blob:\n\t {0}\n", filename);


        }
        public static void SynthesisSsmlToMp3File(string[] paragraphs, string filepath)
        {
            string voiceName = "en-US-AmberNeural";
            // string speakSsml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xml:lang='en-IN'><voice xml:lang='en-IN' xml:gender='Female' name='en-US-AmberNeural'>{paragraphs}</voice></speak>";
            // var config = SpeechConfig.FromSubscription("bd7ffe6b5d114f77bf966ea8ecb7b557", "westus");
            var config = SpeechConfig.FromSubscription(speechSubscriptionKey, coglocation);
            // Sets the synthesis output format.
            // The full list of supported format can be found here:
            // Docs.microsoft.com/azure/cognitive-services/speech-service/rest-text-to-speech#audio-outputs
            // config.SetSpeechSynthesisOutputFormat((SpeechSynthesisOutputFormat)Enum.Parse(typeof(SpeechSynthesisOutputFormat), codec));
            config.SpeechSynthesisVoiceName = voiceName;
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio24Khz96KBitRateMonoMp3);

            // Creates a speech synthesizer using file as audio output.
            // Replace with your own audio file name.
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var fileName = voiceName + ".mp3";
            using (var fileOutput = AudioConfig.FromWavFileOutput(filepath))
            using (var synthesizer = new SpeechSynthesizer(config, fileOutput))
            {
                foreach (string pargraph in paragraphs)
                {
                    string formatPargraph = pargraph.Replace("\\n", string.Empty);
                    //string formatPargraph= pargraph.Replace("\n", "").Replace("\r", "");
                    string speakSsml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xml:lang='hi-IN'><voice xml:lang='hi-IN' xml:gender='Female' name='en-US-AmberNeural'><lexicon uri='{lexiconuri}'/>{formatPargraph}</voice></speak>";
                    var ssml = speakSsml;
                    int retry = 3;
                    while (retry > 0)
                    {
                        using (var result = synthesizer.SpeakSsmlAsync(ssml).Result)
                        {
                            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                            {
                                Console.WriteLine($"success on {voiceName} {result.ResultId} in {sw.ElapsedMilliseconds} msec");
                                break;
                            }
                            else if (result.Reason == ResultReason.Canceled)
                            {
                                Console.WriteLine($"failed on {voiceName}{ssml} {result.ResultId}");
                                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                                if (cancellation.Reason == CancellationReason.Error)
                                {
                                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                                    Console.WriteLine($"CANCELED: Did you update the subscription info?");
                                }
                            }

                            retry--;
                            Console.WriteLine("retrying again...");
                        }
                    }
                }
            }
        }
    }
}

