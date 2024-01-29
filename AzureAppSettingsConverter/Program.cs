using System.Diagnostics;
using System.Text.Json;

Console.WriteLine($"This app converts a configuration dump from Azure App Service (advance edit), into a nested appsettings.json file.{Environment.NewLine}{Environment.NewLine}");

var filePath = string.Empty;

while(string.IsNullOrEmpty(filePath))
{
    try
    {
        filePath = args.Length == 1 ? args[0] : string.Empty;

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("Please enter the path to the Azure App Service configuration file.");
            filePath = Console.ReadLine();
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("File does not exist.");
            filePath = string.Empty;
            args = [];
            continue;
        }

        var appServiceConfig = GetAzureAppServiceConfiguration(filePath);

        var appSettingsJson = ConvertToAppSettingsJson(appServiceConfig);

        // get the directory from the filepath
        var directory = Path.GetDirectoryName(filePath);
        var newFilepath = $"{directory}/appsettings.json";


        File.WriteAllText(newFilepath, appSettingsJson);
        Console.WriteLine($"appsettings.json file created successfully in {directory}");

        Process.Start("notepad.exe", newFilepath);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {Environment.NewLine}{Environment.NewLine}{ex.Message}");
    }
}



static Dictionary<string, string> GetAzureAppServiceConfiguration(string filePath)
{
    var jsonString = File.ReadAllText(filePath);
    var appConfigs = JsonSerializer.Deserialize<List<AppConfigItem>>(jsonString);

    var config = new Dictionary<string, string>();
    
    foreach (var appConfig in appConfigs)    
        config[appConfig.name] = appConfig.value;
    
    return config;
}

static string ConvertToAppSettingsJson(Dictionary<string, string> config)
{
    var formattedConfig = new Dictionary<string, object>();

    foreach (var kvp in config)    
        InsertIntoFormattedConfig(formattedConfig, kvp.Key, kvp.Value);
    
    return JsonSerializer.Serialize(formattedConfig);
}

static void InsertIntoFormattedConfig(Dictionary<string, object> formattedConfig, string key, string value)
{
    var parts = key.Contains(':') ? key.Split(':') : key.Split("__");
    var currentDict = formattedConfig;

    for (int i = 0; i < parts.Length - 1; i++)
    {
        if (!currentDict.ContainsKey(parts[i]))        
            currentDict[parts[i]] = new Dictionary<string, object>();
        
        currentDict = (Dictionary<string, object>)currentDict[parts[i]];
    }

    currentDict[parts[^1]] = value;
}

class AppConfigItem
{
    public string name { get; set; }
    public string value { get; set; }
    public bool slotSetting { get; set; }
}
