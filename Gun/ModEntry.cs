using Gun.Framework;
using Gun.Framework.Patches;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.GameData.Weapons;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.SaveSerialization;

namespace Gun;

public class ModEntry : Mod
{
    private static ModEntry _instance;

    public Config Config;

    public ModEntry()
    {
        _instance = this;
    }

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<Config>();
        helper.Events.Content.AssetRequested += OnAssetRequested;
        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.Patch(
            original: AccessTools.Method(typeof(SaveSerializer), nameof(SaveSerializer.GetSerializer)),
            prefix: new HarmonyMethod(typeof(SaveSerializerPatches), nameof(SaveSerializerPatches.GetSerializer))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(WeaponDataDefinition), nameof(WeaponDataDefinition.CreateItem)),
            postfix: new HarmonyMethod(typeof(WeaponDataDefinitionPatches),
                nameof(WeaponDataDefinitionPatches.CreateItem))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Game1), nameof(Game1.drawTool), new[] { typeof(Farmer) }),
            prefix: new HarmonyMethod(typeof(Game1Patches), nameof(Game1Patches.DrawTool))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(CraftingPage), "layoutRecipes"),
            postfix: new HarmonyMethod(typeof(CraftingPagePatches), nameof(CraftingPagePatches.LayoutRecipes))
        );
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("Data/Weapons"))
        {
            e.Edit(assets =>
            {
                var dict = assets.AsDictionary<string, WeaponData>();
                dict.Data[Framework.Gun.Ak47Id] = new WeaponData
                {
                    Name = Framework.Gun.Ak47Id,
                    DisplayName = "[LocalizedText Strings\\Gun:Gun.Weapon.Ak47.DisplayName]",
                    Description = "[LocalizedText Strings\\Gun:Gun.Weapon.Ak47.Description]",
                    CanBeLostOnDeath = false,
                    Texture = Framework.Gun.Ak47Id
                };
                dict.Data[Framework.Gun.M16Id] = new WeaponData
                {
                    Name = Framework.Gun.M16Id,
                    DisplayName = "[LocalizedText Strings\\Gun:Gun.Weapon.M16.DisplayName]",
                    Description = "[LocalizedText Strings\\Gun:Gun.Weapon.M16.Description]",
                    CanBeLostOnDeath = false,
                    Texture = Framework.Gun.M16Id
                };
            });
        }
        else if (e.Name.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(assets =>
            {
                var dict = assets.AsDictionary<string, ObjectData>();
                dict.Data[Framework.Gun.AmmoAssaultRifleId] = new ObjectData
                {
                    Name = Framework.Gun.AmmoAssaultRifleId,
                    DisplayName = "[LocalizedText Strings\\Gun:Gun.Object.AssaultRifleAmmo.DisplayName]",
                    Description = "[LocalizedText Strings\\Gun:Gun.Object.AssaultRifleAmmo.Description]",
                    Texture = "Gun_Ammo"
                };
            });
        }
        else if (e.Name.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(assets =>
            {
                var dict = assets.AsDictionary<string, string>();
                dict.Data[Framework.Gun.Ak47Id] = $"335 20/Field/{Framework.Gun.Ak47Id}/false/default/";
                dict.Data[Framework.Gun.M16Id] = $"335 20/Field/{Framework.Gun.M16Id}/false/default/";
                dict.Data[Framework.Gun.AmmoAssaultRifleId] =
                    $"382 1 378 1/Field/{Framework.Gun.AmmoAssaultRifleId}/false/default/";
            });
        }
        else if (e.Name.IsEquivalentTo(Framework.Gun.Ak47Id))
        {
            e.LoadFromModFile<Texture2D>("assets/assault_rifle/ak47.png", AssetLoadPriority.Medium);
        }
        else if (e.Name.IsEquivalentTo(Framework.Gun.M16Id))
        {
            e.LoadFromModFile<Texture2D>("assets/assault_rifle/m16.png", AssetLoadPriority.Medium);
        }
        else if (e.Name.IsEquivalentTo("Gun_Ammo"))
        {
            e.LoadFromModFile<Texture2D>("assets/ammo.png", AssetLoadPriority.Medium);
        }
        else if (e.Name.IsEquivalentTo("Strings/Gun"))
        {
            var locale = Helper.Translation.LocaleEnum switch
            {
                LocalizedContentManager.LanguageCode.en => "default",
                _ => Helper.Translation.LocaleEnum.ToString()
            };
            e.LoadFromModFile<Dictionary<string, string>>($"i18n/{locale}.json",
                AssetLoadPriority.Medium);
        }
        else if (e.Name.IsEquivalentTo("Data/Shops"))
        {
            e.Edit(asset =>
            {
                var editor = asset.AsDictionary<string, ShopData>();
                editor.Data["AdventureShop"].Items.AddRange(new[]
                {
                    new ShopItemData
                    {
                        ItemId = Framework.Gun.Ak47Id,
                        Price = Config.Ak47Price
                    },
                    new ShopItemData
                    {
                        ItemId = Framework.Gun.M16Id,
                        Price = Config.M16Price
                    },
                    new ShopItemData
                    {
                        ItemId = Framework.Gun.AmmoAssaultRifleId,
                        Price = Config.AssaultRiflePrice
                    }
                });
            });
        }
    }

    public static ModEntry GetInstance()
    {
        return _instance;
    }
}