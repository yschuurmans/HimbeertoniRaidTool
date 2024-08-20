
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using ImGuiNET;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Plugin.Services;
public class Exporter
{

    private const string BaseFolder = "E:\\SYNC\\Documents\\Gamerelated\\ffxiv\\Raiding\\RaidToolConfigs";

    // Build a Json of the current group, and export it to a file
    // The json Should include:
    // Each character in the group, with their name, world, and class, along with their gear, and an Etro link, if available
    internal static async Task ExportGroupData(RaidGroup currentGroup)
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

        // if UploadToSpreadsheet takes longer than 15 seconds, stop the process
        // UploadToSpreadsheet(json);
        ImGui.SetClipboardText(json);
        //await UploadToSpreadsheet(json);

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
    //private static async Task UploadToSpreadsheet(string groupData)
    //{
    //    var googleConfigLocation = Path.Combine(BaseFolder, "googleConfig.json");
    //    // read config from googleConfig.json to get the below configuration
    //    if (!File.Exists(googleConfigLocation))
    //    {
    //        var newConfig = new
    //        {
    //            GOOGLE_SERVICE_ACCOUNT = "your-service-account",
    //            GOOGLE_SPREADSHEET_ID = "your-spreadsheet-id",
    //            GOOGLE_JSON_CREDS_PATH = "path-to-your-json-creds"
    //        };
    //        var newJson = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
    //        File.WriteAllText(googleConfigLocation, newJson);
    //    }

    //    var json = File.ReadAllText(googleConfigLocation);
    //    var config = JsonConvert.DeserializeObject<GoogleConfig>(json);

    //    if (config == null || config.GOOGLE_SERVICE_ACCOUNT == "your-service-account")
    //    {
    //        Console.WriteLine("Config file is missing or invalid");
    //        return;
    //    }

    //    // Get the Google Spreadsheet Config Values
    //    var serviceAccount = config.GOOGLE_SERVICE_ACCOUNT;
    //    var documentId = config.GOOGLE_SPREADSHEET_ID;
    //    var jsonCredsPath = config.GOOGLE_JSON_CREDS_PATH;

    //    // In this case the json creds file is stored locally, but you can store this however you want to (Azure Key Vault, HSM, etc)
    //    var jsonCredsContent = File.ReadAllText(jsonCredsPath);

    //    // Create a new SheetHelper class
    //    var sheetHelper = new SheetHelper(documentId, serviceAccount, "");
    //    sheetHelper.Init(jsonCredsContent);


    //    var updates = new List<BatchUpdateRequestObject>();
    //    updates.Add(new BatchUpdateRequestObject()
    //    {
    //        Range = new SheetRange("", 24, 53, 24, 53),
    //        Data = new CellData()
    //        {
    //            UserEnteredValue = new ExtendedValue()
    //            {
    //                StringValue = groupData
    //            }
    //        }
    //    });

    //    // Note the field mask parameter not being specified here defaults to => "userEnteredValue"
    //    sheetHelper.BatchUpdate(updates);

    //    await Task.Delay(50);
    //}

    // The above code block is made with a different library. Rewrite the UploadToSpreadsheet method using the Google.Apis.Sheets.v4 library
    private static async Task UploadToSpreadsheet(string groupData)
    {
        // Google Sheets API configuration
        string[] Scopes = { SheetsService.Scope.Spreadsheets };
        string ApplicationName = "Your Application Name";

        var googleConfigLocation = Path.Combine(BaseFolder, "googleConfig.json");

        // Read or create configuration file
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

        // Authorize and create the Sheets API service
        var service = AuthorizeGoogleService(jsonCredsPath, Scopes, ApplicationName);

        // Specify the range in which to update the data
        string range = "X53:X53"; // Adjust the range according to your needs

        // Prepare data to be updated in the sheet
        var values = new List<IList<object>> { new List<object> { groupData } };

        // Call the batch update method to update the sheet
        UpdateGoogleSheet(values, documentId, range, service);

        await Task.Delay(50);
    }

    private static SheetsService AuthorizeGoogleService(string jsonCredsPath, string[] scopes, string applicationName)
    {
        GoogleCredential credential;

        using (var stream = new FileStream(jsonCredsPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream).CreateScoped(scopes);
        }

        // Create Google Sheets API service.
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });

        return service;
    }

    private static void UpdateGoogleSheet(IList<IList<object>> values, string spreadsheetId, string range, SheetsService service)
    {
        var valueRange = new ValueRange
        {
            Values = values
        };

        var request = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        var response = request.Execute();
    }


    private class GoogleConfig
    {
        public string GOOGLE_SERVICE_ACCOUNT { get; set; }
        public string GOOGLE_SPREADSHEET_ID { get; set; }
        public string GOOGLE_JSON_CREDS_PATH { get; set; }
    }
}


//using Google.Apis.Auth.OAuth2;
//using Google.Apis.Sheets.v4;
//using Google.Apis.Sheets.v4.Data;
//using Google.Apis.Services;
//using Google.Apis.Util.Store;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Data.OleDb;
//using System.Data;

//namespace testGoogleSheets
//{
//    public class Attendance
//    {
//        public string AttendanceId { get; set; }
//    }

//    class Program
//    {
//        // If modifying these scopes, delete your previously saved credentials
//        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
//        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
//        static string ApplicationName = "TimeSheetUpdation By Cybria Technology";
//        static string SheetId = "Your sheet id";

//        static void Main(string[] args)
//        {
//            var service = AuthorizeGoogleApp();

//            string newRange = GetRange(service);

//            IList<IList<Object>> objNeRecords = GenerateData();

//            UpdatGoogleSheetinBatch(objNeRecords, SheetId, newRange, service);

//            Console.WriteLine("Inserted");
//            Console.Read();
//        }

//        private static SheetsService AuthorizeGoogleApp()
//        {
//            UserCredential credential;

//            using (var stream =
//                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
//            {
//                string credPath = System.Environment.GetFolderPath(
//                    System.Environment.SpecialFolder.Personal);
//                credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json");

//                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
//                    GoogleClientSecrets.Load(stream).Secrets,
//                    Scopes,
//                    "user",
//                    CancellationToken.None,
//                    new FileDataStore(credPath, true)).Result;
//                Console.WriteLine("Credential file saved to: " + credPath);
//            }

//            // Create Google Sheets API service.
//            var service = new SheetsService(new BaseClientService.Initializer()
//            {
//                HttpClientInitializer = credential,
//                ApplicationName = ApplicationName,
//            });

//            return service;
//        }

//        protected static string GetRange(SheetsService service)
//        {
//            // Define request parameters.
//            String spreadsheetId = SheetId;
//            String range = "A:A";

//            SpreadsheetsResource.ValuesResource.GetRequest getRequest =
//                       service.Spreadsheets.Values.Get(spreadsheetId, range);

//            ValueRange getResponse = getRequest.Execute();
//            IList<IList<Object>> getValues = getResponse.Values;

//            int currentCount = getValues.Count() + 2;

//            String newRange = "A" + currentCount + ":A";

//            return newRange;
//        }

//        private static IList<IList<Object>> GenerateData()
//        {
//            List<IList<Object>> objNewRecords = new List<IList<Object>>();

//            IList<Object> obj = new List<Object>();

//            obj.Add("Column - 1");
//            obj.Add("Column - 2");
//            obj.Add("Column - 3");

//            objNewRecords.Add(obj);

//            return objNewRecords;
//        }

//        private static void UpdatGoogleSheetinBatch(IList<IList<Object>> values, string spreadsheetId, string newRange, SheetsService service)
//        {
//            SpreadsheetsResource.ValuesResource.AppendRequest request =
//               service.Spreadsheets.Values.Append(new ValueRange() { Values = values }, spreadsheetId, newRange);
//            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
//            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
//            var response = request.Execute();
//        }

//    }
//}