using System.Xml.Serialization;
using StardewValley;
using StardewValley.Inventories;

namespace Firearm.Framework.Patches;

public class SaveSerializerPatches {
    
    private static readonly XmlSerializer ItemSerializer = new(typeof(Item),new []{typeof(Firearm)});
    
    internal static bool GetSerializer(ref XmlSerializer __result,Type type)
    {
        if (type != typeof(Item)) return true;
        __result = ItemSerializer;
        return false;
    }
}