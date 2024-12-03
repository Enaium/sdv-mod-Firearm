using System.Xml.Serialization;
using StardewValley;
using StardewValley.GameData.Tools;
using StardewValley.GameData.Weapons;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;

namespace Firearm.Framework.Patches;

public class WeaponDataDefinitionPatches
{
    internal static void CreateItem(ref Item __result, ParsedItemData data)
    {
        __result = data.ItemId switch
        {
            Firearm.Ak47Id => new Firearm(Firearm.Ak47Id),
            Firearm.M16Id => new Firearm(Firearm.M16Id),
            _ => __result
        };
    }
}