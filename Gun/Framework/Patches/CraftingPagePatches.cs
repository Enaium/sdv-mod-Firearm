using StardewValley.Menus;

namespace Gun.Framework.Patches;

public class CraftingPagePatches
{
    internal static void LayoutRecipes(CraftingPage __instance)
    {
        var instancePagesOfCraftingRecipe = __instance.pagesOfCraftingRecipes[0];
        foreach (var (clickableTextureComponent, craftingRecipe) in instancePagesOfCraftingRecipe)
        {
            if (!new[] { Gun.Ak47Id, Gun.M16Id }.Contains(craftingRecipe.name)) continue;
            clickableTextureComponent.sourceRect.Width += 16;
            clickableTextureComponent.sourceRect.Height += 16;
        }
    }
}