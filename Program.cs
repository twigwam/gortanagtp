using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio; 
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Globalization;
using System.Web;
using System.Reflection;
using Newtonsoft.Json;
using System.Diagnostics;

namespace SpeechToText
{
    class Program
    {
        // AZURE 
        static string YourServiceRegion = "xxxxxxxxxxx";
        static string YourSubscriptionKey = "xxxxxxxxx"; // change to your azure cognitive key
       // static string endpoint = "xxxxxxxxxxxxxxxxxx"; 
        private static string _cultureLng = "en-US"; // https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.getcultures?view=net-7.0
        private static string _synthVoice = "en-US-JennyNeural"; // https://github.com/microsoft/cognitive-services-speech-sdk-js/blob/master/src/sdk/SpeechSynthesizer.ts
        
        // OPEN AI
        private static string _txtContext = "Tell me something funny about:";
        static string _oiKey = "xxxxxxxxxxxxx"; // change to your key
        static string _oAICompletion = "https://api.openai.com/v1/completions";
        static string _oAIModeration = "https://api.openai.com/v1/moderations";
        static string _oAIImages = "https://api.openai.com/v1/images/generations"; 
        static string _engineAI = "text-davinci-003";

        private static readonly HttpClient client = new HttpClient();
       // static string urlChat = "http://127.0.0.1:5001/chat?q=";
        static string _env, _folderPath = "";

        static async Task Main(string[] args)
        {  
            cmdWelcomeBot();   

            if (_env == "1")
            { 
                await Program.SynthesisToSpeakerAsync("Hello, How may I help you today?");
                await Program.ConversationStart();
            }
            else
            {
                Console.WriteLine("Start conversation...");
                string val = Console.ReadLine();
                await cmdDialog(val);  
            }
        }


      static  async  Task ConversationStart()
        {
            //  Console.Clear();
            string _generatedContent = "";
            CultureInfo ci = new CultureInfo(_cultureLng);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // speech 2 text  azure way
            var speechConfig = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
            // The language of the voice that speaks.
            speechConfig.SpeechSynthesisVoiceName = _synthVoice; 
            speechConfig.SpeechRecognitionLanguage = _cultureLng;

            string myRequest = await FromMic(speechConfig);
            if (myRequest != "")
            {
                // ChatGTP - API call 
                // string _chatResponse = CallApiWithGet(urlChat + myRequest);
            
                // OPEN AI - GTP call 
                // var task = cmdModerateAsync(myRequest); 
                var task = cmdGenerateContentAsync(myRequest);
                _generatedContent = await task;

                // var resultString = Regex.Replace(_generatedContet.ToString(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
                //  openai/ chat response
               // Console.WriteLine($"Confirmed :{myRequest}");

                // speak back to user with voice using azure text 2 speech
                await SynthesisToSpeakerAsync(_generatedContent);

            }
            // loop back 
            await ConversationStart();
        }



        // call to API chat 
        static string CallApiWithGet(string url)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    return null;
                }
            }
        }
         
  
        // this will convert the text into audio
        public static async Task SynthesisToSpeakerAsync(string text)
        {
          
            Console.WriteLine($"Speech synthesized: {text}");
             
            SpeechConfig config = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion); 
            config.SpeechSynthesisLanguage = _cultureLng; 
            config.SpeechSynthesisVoiceName = _synthVoice;
          //  config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio48Khz192KBitRateMonoMp3);
            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer(config))
            {
                using (SpeechSynthesisResult result = await synthesizer.SpeakTextAsync(text))
                {
                    if (result.Reason != ResultReason.SynthesizingAudioCompleted) ; 
                }
            }
            
        }

        // this will save the audio file (optional)
        static async Task SynthesizeFileAudioAsync(string _fileName, string _text)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(_cultureLng);
            var config = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
            config.SpeechSynthesisVoiceName = _synthVoice;
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio48Khz192KBitRateMonoMp3);
            //  https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/rest-text-to-speech?tabs=streaming#audio-outputs
            var audioConfig = AudioConfig.FromWavFileOutput(_folderPath +"/"+ _fileName + ".mp3");
            var synthesizer = new SpeechSynthesizer(config, audioConfig);
            await synthesizer.SpeakTextAsync(_text);
        }
          
        // this will convert your voice in text 
        async static Task<string> FromMic(SpeechConfig speechConfig)
        { 
            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            var recognizer = new SpeechRecognizer(speechConfig, audioConfig); 
            Console.WriteLine("Say something..");
            var result = await recognizer.RecognizeOnceAsync();
            string strResult = result.Text; 

            Console.WriteLine( $": {strResult}");
            return strResult;
        }

        public static string cleanStr(string input)
        {
            Regex r = new Regex("(?:[^a-z0-9 ]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            return r.Replace(input, String.Empty);
        }

        static async Task<string> cmdGenerateContentAsync(string _keyTopic)
        {
          
             string _resultGen = "";
             string _prompt = _txtContext +" " + _keyTopic +". Be short and concise.";  // you can define context here
           
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_oAICompletion),
                Headers = { { "Authorization", "Bearer " + _oiKey }, },
                Content = new StringContent("{\n  \"model\": \"" + _engineAI + "\",\n\t\"prompt\": \"" + _prompt + "\",\n\t\"max_tokens\": 45,\n\t\"temperature\": 1,\n\t\"frequency_penalty\": 0,\n\t\"presence_penalty\": 0,\n\t\"top_p\": 1,\n\t\"stop\": null\n}")
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
                }
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                JToken o = JObject.Parse(body);
                // string myResult = deserialized.SelectToken("choices").ToString();
                JArray _content = (JArray)o["choices"];
                _resultGen = (string)_content[0]["text"].ToString();
                // _resultGen = (myResult);
            }

            return _resultGen;

        }



        static async Task<string> cmdGenerateImageAsync(string _prompt, int _NoImg)
        {
            string _resultGen = "";
            string _fileLocalName = "";
            string _fileRandomName = System.IO.Path.GetRandomFileName(); // this will generate random file name
            string _tempDire = System.IO.Path.GetTempPath(); /*Server.MapPath("")*/; // System.IO.Path.GetTempPath();   
            _fileLocalName = _tempDire + _fileRandomName + ".png";
            string _fileLocalJPGName = "";
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_oAIImages),
                Headers = { { "Authorization", "Bearer " +  _oiKey }, },     //  256x256    512x512   1024x1024
                Content = new StringContent("{\n    \"prompt\": \"" + _prompt + "\",\n    \"n\": " + _NoImg + ",\n    \"size\": \"512x512\" \n}")
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
                }
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                JToken o = JObject.Parse(body);
                // _resultGen = o.SelectToken("data").ToString();

                JArray _content = (JArray)o["data"];
                _resultGen = (string)_content[0]["url"].ToString();

                string _picUrl = _resultGen;

                using (WebClient downClient = new WebClient())
                {
                    // downClient.DownloadFile(new Uri(_picUrl), _fileLocalName);
                    // OR 
                    downClient.DownloadFileAsync(new Uri(_picUrl), _fileLocalName);
                }

                //Image png = Image.FromFile(_fileLocalName);
                //_fileLocalJPGName = _tempDire + @"/" + _fileRandomName + ".jpg";
                //png.Save(_tempDire, ImageFormat.Jpeg);
                //png.Dispose(); 
                //   File.Delete(_fileLocalName);
            }
            return _fileLocalName;
        }

        static async Task<bool> cmdModerateAsync(string _input)
        {
            bool _statusResponse = false;

            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_oAIModeration),
                Headers =
                     {
                       { "Authorization", "Bearer "+ _oiKey },
                     },
                Content = new StringContent("{\"input\": \"" + _input + "\"}")
                { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } }
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                JToken deserialized = JObject.Parse(body);
                string myResult = deserialized.SelectToken("results").ToString();

                if (myResult.Contains("true")) { _statusResponse = true; }
            }

            return _statusResponse;

        }


        private static async Task cmdDialog(string myRequest)
        {
            var task = cmdGenerateContentAsync(myRequest);
            string _generatedContent = await task;

            await  SynthesisToSpeakerAsync(_generatedContent);
            string val = Console.ReadLine();
            await cmdDialog(val); 
        }


        static void cmdWelcomeBot() {

            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(@"     .-'`'-.");
            Console.WriteLine(@"    /  O   O \");
            Console.WriteLine(@"   /   ----   \");
            Console.WriteLine(@"  /   /    \   \");
            Console.WriteLine(@" /___/      \___\");
            Console.WriteLine("Press 1 for voice or 2 for texting...");

            _env = Console.ReadLine();


        }



    }
}



//try
//{
//    // you add some context from the training file 
//    _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop).ToString();
//    string _trainFile = _folderPath + "/Training.txt";
//    using (StreamReader streamReader = new StreamReader(_trainFile, Encoding.UTF8))
//        _txtContext = cleanStr(streamReader.ReadToEnd());
//    Console.WriteLine("Training loaded...");
//}
//catch
//{
//    Console.WriteLine("Training.txt file missing from desktop... loading dialog without any context.");
//}