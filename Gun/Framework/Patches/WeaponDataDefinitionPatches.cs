using System.Xml.Serialization;
using StardewValley;
using StardewValley.GameData.Tools;
using StardewValley.GameData.Weapons;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;

namespace Gun.Framework.Patches;

public class WeaponDataDefinitionPatches
{
    internal static void CreateItem(ref Item __result, ParsedItemData data)
    {
        __result = data.ItemId switch
        {
            Gun.Ak47Id => new Gun(Gun.Ak47Id),
            Gun.M16Id => new Gun(Gun.M16Id),
            _ => __result
        };
    }
}