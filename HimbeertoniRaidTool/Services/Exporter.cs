using Newtonsoft.Json;
using System.IO;

namespace HimbeertoniRaidTool.Plugin.Services;
public class Exporter
{
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
        File.WriteAllText("groupData.json", json);
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
}
