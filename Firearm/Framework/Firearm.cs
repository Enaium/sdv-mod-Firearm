using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley;
using StardewValley.Projectiles;
using Object = StardewValley.Object;

namespace Firearm.Framework;

[XmlType("Mods_Enaium_Firearm")]
public sealed class Firearm : Tool
{
    [XmlIgnore]
    private bool _using;

    [XmlIgnore]
    private readonly NetEvent0 _finishEvent = new();

    [XmlIgnore]
    private readonly NetPoint _aimPos = new NetPoint().Interpolated(true, true);

    [XmlIgnore]
    private double _lastFireTime;

    public const string Ak47Id = "Firearm_AK47";
    public const string M16Id = "Firearm_M16";
    public const string AmmoAssaultRifleId = "Firearm_Ammo_Assault_Rifle";


    public Firearm() : this(Ak47Id)
    {
    }

    public Firearm(string id)
    {
        id = ValidateUnqualifiedItemId(id);
        var dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(W)" + itemId);
        ItemId = id;
        Name = dataOrErrorItem.InternalName;
        InitialParentTileIndex = dataOrErrorItem.SpriteIndex;
        CurrentParentTileIndex = dataOrErrorItem.SpriteIndex;
        IndexOfMenuItemView = dataOrErrorItem.SpriteIndex;
        numAttachmentSlots.Value = 1;
        attachments.SetCount(1);
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(_finishEvent, "finishEvent").AddField(_aimPos, "aimPos");
        _finishEvent.onEvent += DoFinish;
    }

    public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
    {
        who.canReleaseTool = false;
        _using = true;
        return true;
    }

    public override void tickUpdate(GameTime time, Farmer who)
    {
        lastUser = who;
        _finishEvent.Poll();
        if (!_using)
            return;
        if (!who.IsLocalPlayer)
            return;

        var x = _aimPos.X;
        var y = _aimPos.Y;
        var shootOrigin = GetShootOrigin(who);
        var vector2 = AdjustForHeight(new Vector2(x, y)) - shootOrigin;
        if (!_using) return;
        UpdateAimPos();
        if (Math.Abs(vector2.X) > Math.Abs(vector2.Y))
        {
            if (vector2.X < 0.0)
                who.faceDirection(3);
            if (vector2.X > 0.0)
                who.faceDirection(1);
        }
        else
        {
            if (vector2.Y < 0.0)
                who.faceDirection(0);
            if (vector2.Y > 0.0)
                who.faceDirection(2);
        }

        DoFire(who, x, y, shootOrigin);
    }

    private void DoFire(Farmer who, int x, int y, Vector2 shootOrigin)
    {
        var attachment = attachments[0];
        if (attachment == null)
        {
            Game1.addHUDMessage(
                new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14254"), 3)
                {
                    timeLeft = 100
                });
            return;
        }

        if (_lastFireTime + 1000 / (GetShotSpeed() / 60f) >= Game1.currentGameTime.TotalGameTime.TotalMilliseconds)
            return;

        var one = (Object)attachment.getOne();
        if (attachment.ConsumeStack(1) == null)
            attachments[0] = null;

        NetCollection<Projectile> projectiles = who.currentLocation.projectiles;
        var ammoDamage = GetAmmoDamage(one);
        var velocityTowardPoint = Utility.getVelocityTowardPoint(GetShootOrigin(who),
            AdjustForHeight(new Vector2(x, y)),
            (15 + Game1.random.Next(4, 6)) * (1f + who.buffs.WeaponSpeedMultiplier));
        var basicProjectile = new BasicProjectile(
            (int)((ammoDamage + Game1.random.Next(-(ammoDamage / 2), ammoDamage + 2)) *
                  (1.0 + who.buffs.AttackMultiplier)), -1, 0, 0, 0, velocityTowardPoint.X, velocityTowardPoint.Y,
            shootOrigin - new Vector2(32f, lastUser.FacingDirection != 0 ? 32f : 96f), damagesMonsters: true,
            location: who.currentLocation, firer: who, shotItemId: one.ItemId)
        {
            IgnoreLocationCollision = Game1.currentLocation.currentEvent != null || Game1.currentMinigame != null
        };

        _lastFireTime = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        projectiles.Add(basicProjectile);
        who.playNearbySoundAll(GetAudioName());
    }

    private int GetAmmoDamage(Object one)
    {
        return one switch
        {
            { Name: AmmoAssaultRifleId } => ModEntry.GetInstance().Config.AssaultRifleDamage,
            _ => 0
        };
    }

    private string GetAudioName()
    {
        return ItemId switch
        {
            Ak47Id => ModEntry.GetInstance().Config.Ak47ShotAudio,
            M16Id => ModEntry.GetInstance().Config.M16ShotAudio,
            _ => ""
        };
    }

    private int GetShotSpeed()
    {
        return ItemId switch
        {
            Ak47Id => ModEntry.GetInstance().Config.Ak47ShotSpeed,
            M16Id => ModEntry.GetInstance().Config.M16ShotSpeed,
            _ => 0
        };
    }

    public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
    {
        _finishEvent.Fire();
    }

    public override bool onRelease(GameLocation location, int x, int y, Farmer who)
    {
        DoFunction(location, x, y, 1, who);
        return true;
    }

    private void DoFinish()
    {
        if (lastUser == null)
            return;
        lastUser.usingSlingshot = false;
        lastUser.canReleaseTool = true;
        lastUser.UsingTool = false;
        lastUser.canMove = true;
        lastUser.Halt();
        _using = false;
    }

    public override void draw(SpriteBatch b)
    {
        var dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(QualifiedItemId);
        var position = lastUser.Position;
        var spriteEffects = SpriteEffects.None;

        position.X += 32f;
        position.Y -= 16f;

        var x = _aimPos.X;
        var y = _aimPos.Y;

        float rotation;

        if (lastUser.FacingDirection == 3)
        {
            spriteEffects = SpriteEffects.FlipVertically;
            rotation = (float)Math.PI / 1.3f;
            rotation += (float)Math.Atan2(position.Y - y, position.X - x);
        }
        else
        {
            rotation = MathHelper.PiOver4;
            rotation += (float)Math.Atan2(y - position.Y, x - position.X);
        }

        if (lastUser.FacingDirection != 0)
            b.Draw(dataOrErrorItem.GetTexture(),
                Game1.GlobalToLocal(Game1.viewport, position),
                new Rectangle(0, 0, 32, 32), Color.White, rotation,
                new Vector2(32 / 2f, 32 / 2f),
                3f, spriteEffects, 0.999999f);
    }

    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency,
        float layerDepth,
        StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        var dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(QualifiedItemId);
        spriteBatch.Draw(dataOrErrorItem.GetTexture(), location + new Vector2(32f, 29f),
            Game1.getSourceRectForStandardTileSheet(dataOrErrorItem.GetTexture(), 0, 32, 32), color * transparency,
            0.0f, new Vector2(14f, 14f), scaleSize * 2f, SpriteEffects.None, layerDepth);
        if (drawStackNumber != StackDrawType.Hide && attachments?[0] != null)
            Utility.drawTinyDigits(attachments[0].Stack, spriteBatch,
                location + new Vector2(
                    64 - Utility.getWidthOfTinyDigitString(attachments[0].Stack, 3f * scaleSize) + 3f * scaleSize,
                    (float)(64.0 - 18.0 * scaleSize + 2.0)), 3f * scaleSize, 1f, Color.White);
        DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
    }

    private void UpdateAimPos()
    {
        if (lastUser is not { IsLocalPlayer: true })
            return;
        var point = Game1.getMousePosition();
        if (Game1.options.gamepadControls && !Game1.lastCursorMotionWasMouse)
        {
            var vector2 = Game1.oldPadState.ThumbSticks.Left;
            if (vector2.Length() < 0.25)
            {
                vector2.X = 0.0f;
                vector2.Y = 0.0f;
                var dpad = Game1.oldPadState.DPad;
                if (dpad.Down == ButtonState.Pressed)
                {
                    vector2.Y = -1f;
                }
                else
                {
                    dpad = Game1.oldPadState.DPad;
                    if (dpad.Up == ButtonState.Pressed)
                        vector2.Y = 1f;
                }

                dpad = Game1.oldPadState.DPad;
                if (dpad.Left == ButtonState.Pressed)
                    vector2.X = -1f;
                dpad = Game1.oldPadState.DPad;
                if (dpad.Right == ButtonState.Pressed)
                    vector2.X = 1f;
                if (vector2.X != 0.0 && vector2.Y != 0.0)
                {
                    vector2.Normalize();
                    vector2 *= 1f;
                }
            }

            Vector2 shootOrigin = GetShootOrigin(lastUser);
            if (!Game1.options.useLegacySlingshotFiring && vector2.Length() < 0.25)
            {
                vector2 = lastUser.FacingDirection switch
                {
                    0 => new Vector2(0.0f, 1f),
                    1 => new Vector2(1f, 0.0f),
                    2 => new Vector2(0.0f, -1f),
                    3 => new Vector2(-1f, 0.0f),
                    _ => vector2
                };
            }

            point = Utility.Vector2ToPoint(shootOrigin + new Vector2(vector2.X, -vector2.Y) * 600f);
            point.X -= Game1.viewport.X;
            point.Y -= Game1.viewport.Y;
        }

        var num1 = point.X + Game1.viewport.X;
        var num2 = point.Y + Game1.viewport.Y;
        _aimPos.X = num1;
        _aimPos.Y = num2;
    }

    protected override void GetAttachmentSlotSprite(
        int slot,
        out Texture2D texture,
        out Rectangle sourceRect)
    {
        base.GetAttachmentSlotSprite(slot, out texture, out sourceRect);
        if (attachments[0] != null)
            return;
        sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 43);
    }

    protected override bool canThisBeAttached(Object o, int slot)
    {
        return ItemId switch
        {
            Ak47Id or M16Id => o.Name == AmmoAssaultRifleId,
            _ => false
        };
    }

    public override string? getHoverBoxText(Item? hoveredItem)
    {
        if (hoveredItem is Object o && canThisBeAttached(o))
            return Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14256", DisplayName,
                o.DisplayName);
        return hoveredItem == null && attachments?[0] != null
            ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14258", attachments[0].DisplayName)
            : null;
    }

    public override string TypeDefinitionId => "(W)";

    protected override void MigrateLegacyItemId()
    {
        ItemId = InitialParentTileIndex.ToString();
    }

    protected override Item GetOneNew()
    {
        return new Firearm(ItemId);
    }

    protected override string loadDisplayName()
    {
        return ItemRegistry.GetDataOrErrorItem(QualifiedItemId).DisplayName;
    }

    protected override string loadDescription()
    {
        return ItemRegistry.GetDataOrErrorItem(QualifiedItemId).Description;
    }

    public override bool doesShowTileLocationMarker() => false;

    private Vector2 GetShootOrigin(Farmer who)
    {
        return AdjustForHeight(who.getStandingPosition(), false);
    }

    private Vector2 AdjustForHeight(Vector2 position, bool forCursor = true)
    {
        return !Game1.options.useLegacySlingshotFiring & forCursor
            ? new Vector2(position.X, position.Y)
            : new Vector2(position.X, (float)(position.Y - 32.0 - 8.0));
    }
}