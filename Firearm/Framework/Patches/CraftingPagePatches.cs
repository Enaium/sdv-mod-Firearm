using StardewValley.Menus;

namespace Firearm.Framework.Patches;

public class CraftingPagePatches
{
    internal static void LayoutRecipes(CraftingPage __instance)
    {
        var instancePagesOfCraftingRecipe = __instance.pagesOfCraftingRecipes[0];
        foreach (var (clickableTextureComponent, craftingRecipe) in instancePagesOfCraftingRecipe)
        {
            if (!new[] { Firearm.Ak47Id, Firearm.M16Id }.Contains(craftingRecipe.name)) continue;
            clickableTextureComponent.sourceRect.Width += 16;
            clickableTextureComponent.sourceRect.Height += 16;
        }
    }
}