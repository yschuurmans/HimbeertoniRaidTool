using Google.Apis.Sheets.v4.Data;
using GoogleSheetsWrapper;
using Newtonsoft.Json;
using System.IO;

namespace HimbeertoniRaidTool.Plugin.Services;
public class Exporter
{

    private const string BaseFolder = "E:\\SYNC\\Documents\\Gamerelated\\ffxiv\\Raiding\\RaidToolConfigs";

    // Build a Json of the current group, and export it to a file
    // The json Should include:
    // Each character in the group, with their name, world, and class, along with their gear, and an Etro link, if available
    internal static void ExportGroupData(RaidGroup currentGroup)
    {
        if (currentGroup == null) return;

        var usefullData = currentGroup.Select(x => x.MainChar).Select(x => new
        {
            Name = x.Name,
            MainClass = x.MainClass?.Name,
            Gear = x.MainClass.CurGear.Select(MapItem),
            BisLink = x.MainClass?.BisSets.FirstOrDefault()?.ExternalId
        });

        var json = JsonConvert.SerializeObject(usefullData, Formatting.Indented);
        var fileLocation = Path.Combine(BaseFolder, "groupData.json");
        File.WriteAllText(fileLocation, json);

        UploadToSpreadsheet(json);
    }

    private static object MapItem(GearItem item, int slotId)
    {
        var slotName = item.Slots.FirstOrDefault().FriendlyName(true);
        if (slotName == "Ring (R)" && slotId % 2 == 1)
        {
            slotName = "Ring (L)";
        }


        return new
        {
            Slot = slotName,
            Source = item.Source.ToString(),
            Name = item.Name,
            ItemLevel = item.ItemLevel
        };
    }

    // using Google sheets, upload the input string to a specific cell in the spreadsheet
    private static void UploadToSpreadsheet(string groupData)
    {
        var googleConfigLocation = Path.Combine(BaseFolder, "googleConfig.json");
        // read config from googleConfig.json to get the below configuration
        if (!File.Exists(googleConfigLocation))
        {
            var newConfig = new
            {
                GOOGLE_SERVICE_ACCOUNT = "your-service-account",
                GOOGLE_SPREADSHEET_ID = "your-spreadsheet-id",
                GOOGLE_JSON_CREDS_PATH = "path-to-your-json-creds"
            };
            var newJson = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
            File.WriteAllText(googleConfigLocation, newJson);
        }

        var json = File.ReadAllText(googleConfigLocation);
        var config = JsonConvert.DeserializeObject<GoogleConfig>(json);

        if (config == null || config.GOOGLE_SERVICE_ACCOUNT == "your-service-account")
        {
            Console.WriteLine("Config file is missing or invalid");
            return;
        }

        // Get the Google Spreadsheet Config Values
        var serviceAccount = config.GOOGLE_SERVICE_ACCOUNT;
        var documentId = config.GOOGLE_SPREADSHEET_ID;
        var jsonCredsPath = config.GOOGLE_JSON_CREDS_PATH;

        // In this case the json creds file is stored locally, but you can store this however you want to (Azure Key Vault, HSM, etc)
        var jsonCredsContent = File.ReadAllText(jsonCredsPath);

        // Create a new SheetHelper class
        var sheetHelper = new SheetHelper(documentId, serviceAccount, "");
        sheetHelper.Init(jsonCredsContent);


        var updates = new List<BatchUpdateRequestObject>();
        updates.Add(new BatchUpdateRequestObject()
        {
            Range = new SheetRange("", 24, 53, 24, 53),
            Data = new CellData()
            {
                UserEnteredValue = new ExtendedValue()
                {
                    StringValue = groupData
                }
            }
        });

        // Note the field mask parameter not being specified here defaults to => "userEnteredValue"
        sheetHelper.BatchUpdate(updates);
    }

    private class GoogleConfig
    {
        public string GOOGLE_SERVICE_ACCOUNT { get; set; }
        public string GOOGLE_SPREADSHEET_ID { get; set; }
        public string GOOGLE_JSON_CREDS_PATH { get; set; }
    }
}


//using System;
//using System.IO;
//using Microsoft.Extensions.Configuration;
//using GoogleSheetsWrapper.SampleClient.SampleModel;
//using CsvHelper.Configuration;
//using System.Globalization;

//namespace GoogleSheetsWrapper.SampleClient
//{
//    public class Program
//    {
//        public static void Main()
//        {
//            var config = BuildConfig();

//            // Get the Google Spreadsheet Config Values
//            var serviceAccount = config["GOOGLE_SERVICE_ACCOUNT"];
//            var documentId = config["GOOGLE_SPREADSHEET_ID"];
//            var jsonCredsPath = config["GOOGLE_JSON_CREDS_PATH"];

//            // In this case the json creds file is stored locally, but you can store this however you want to (Azure Key Vault, HSM, etc)
//            var jsonCredsContent = File.ReadAllText(jsonCredsPath);

//            // Create a new SheetHelper class
//            var sheetHelper = new SheetHelper<SampleRecord>(documentId, serviceAccount, "");
//            sheetHelper.Init(jsonCredsContent);

//            var csvConfig = new CsvConfiguration(CultureInfo.CurrentCulture)
//            {
//                Delimiter = ";",
//            };

//            var repoConfig = new BaseRepositoryConfiguration()
//            {
//                // Does the table have a header row?
//                HasHeaderRow = true,
//                // Are there any blank rows before the header row starts?
//                HeaderRowOffset = 0,
//                // Are there any blank rows before the first row in the data table starts?                
//                DataTableRowOffset = 0,
//            };

//            var respository = new SampleRepository(sheetHelper, repoConfig);

//            var records = respository.GetAllRecords();

//            foreach (var record in records)
//            {
//                try
//                {
//                    Foo(record.TaskName);
//                    record.Result = true;
//                    record.DateExecuted = DateTime.UtcNow;

//                    var result = respository.SaveFields(
//                        record,
//                        r => r.Result,
//                        r => r.DateExecuted);
//                }
//                catch (Exception ex)
//                {
//                    record.Result = false;
//                    record.ErrorMessage = ex.Message;
//                    record.DateExecuted = DateTime.UtcNow;

//                    var result = respository.SaveFields(
//                        record,
//                        r => r.Result,
//                        r => r.ErrorMessage,
//                        r => r.DateExecuted);
//                }
//            }
//        }

//        private static void Foo(string name)
//        {
//            // Do some operation based on the record
//            Console.WriteLine(name);
//        }

//        /// <summary>
//        /// Really simple method to build the config locally.  This requires you to setup User Secrets locally with Visual Studio
//        /// </summary>
//        /// <returns></returns>
//        private static IConfigurationRoot BuildConfig()
//        {
//            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

//            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
//                                devEnvironmentVariable.Equals("development", StringComparison.OrdinalIgnoreCase);

//            var builder = new ConfigurationBuilder();
//            // tell the builder to look for the appsettings.json file
//            _ = builder
//                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

//            //only add secrets in development
//            if (isDevelopment)
//            {
//                _ = builder.AddUserSecrets<Program>();
//            }

//            return builder.Build();
//        }
//    }
//}