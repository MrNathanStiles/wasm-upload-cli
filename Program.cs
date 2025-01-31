// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;


/*
omfg
14mb
dotnet publish -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=true --output "C:\abc"
155kb
dotnet publish -r win-x64 -p:PublishSingleFile=True --self-contained false --output "C:\abc"
*/
public class Config {
    public required string ScriptFile { get; set; }
    public required string ApiKey { get; set; }
    public required string ServerUrl { get; set; }
    
}


public static class Program
{
    const string CONFIG_FILE = "wasm-upload-cli.json";
    
    const string xSERVER_URL = "https://localhost:7086/api/Wasm/Upload/";
    const string SERVER_URL = "http://www.d1ag0n.com/api/Wasm/Upload/";
    const string SCRIPT_FILE = "script.wasm";
    private static string ReadLine() {
        return $"{Console.ReadLine()}";
    }
    private static void WriteLine(string line) {
        Console.WriteLine(line);
    }

    
    public static async Task Main(string[] args)
    {
        var configPath = Path.Combine(Environment.CurrentDirectory, CONFIG_FILE);
            
        if (!File.Exists(configPath))
        {
            WriteLine($"Config file {CONFIG_FILE} not found. Would you like to create it? (y/N)");
            if (ReadLine().ToLowerInvariant() != "y") return;
            WriteLine("Script name? (script.wasm)");
            var scriptFile = ReadLine();

            if (string.IsNullOrWhiteSpace(scriptFile)) scriptFile = SCRIPT_FILE;

            WriteLine("Enter API key.");
            var apiKey = ReadLine().Trim();
            if (string.IsNullOrWhiteSpace(apiKey)) return;

            WriteLine($"Enter server URL. ({SERVER_URL})");
            var serverUrl = ReadLine();
            if (string.IsNullOrWhiteSpace(serverUrl)) serverUrl = SERVER_URL;

            WriteLine("");
            WriteLine($"Script File : {scriptFile}");
            WriteLine($"API Key     : {apiKey}");
            WriteLine($"Server URL  : {serverUrl}");
            WriteLine("");

            
            WriteLine($"Config file will be written to: '{configPath}'");
            
            WriteLine("Does this look correct? (y/N)");

            if (ReadLine().ToLowerInvariant() != "y") return;
            
            var newConfig = new Config {
                ApiKey = apiKey,
                ScriptFile = scriptFile,
                ServerUrl = serverUrl
            };
            var options = new JsonSerializerOptions {
                WriteIndented = true
            };
            File.WriteAllText(configPath, JsonSerializer.Serialize(newConfig, options));
            WriteLine($"Config file written to '{configPath}'.");
            return;
        }
        
        using var client = new HttpClient();
        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
        if (config == null) {
            WriteLine("Configfile not loaded.");
            return;
        }
        using var form = new MultipartFormDataContent();

        var bytes = File.ReadAllBytes(config.ScriptFile);
        form.Add(new ByteArrayContent(bytes), "WASM", "script.wasm");
        
        var result = await client.PostAsync(config.ServerUrl + config.ApiKey, form);

        if (result.StatusCode == HttpStatusCode.OK)
        {
            var localHash = "SHA256-" + Convert.ToHexString(SHA256.HashData(bytes));
            using var stream = result.Content.ReadAsStream();
            using var reader = new StreamReader(stream);
            var remoteHash = reader.ReadToEnd();
            if (localHash == remoteHash) {
                WriteLine("Hash match. Good upload.");
            }
            else
            {
                WriteLine("Hash mismatch. Bad upload.");
            }
        }
        
    }
}