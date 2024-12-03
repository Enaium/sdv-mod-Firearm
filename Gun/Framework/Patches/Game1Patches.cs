using StardewValley;

namespace Gun.Framework.Patches;

public class Game1Patches
{
    internal static bool DrawTool(Farmer f)
    {
        if (f.CurrentTool is not Gun)
            return true;

        f.CurrentTool.draw(Game1.spriteBatch);
        return false;
    }
}