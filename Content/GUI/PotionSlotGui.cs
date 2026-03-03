using PotionSlots.Core.Loaders.UILoading;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace PotionSlots.Content.GUI
{
    public class PotionSlotGui : SmartUIState
    {
        private LifeSlot life;
        private ManaSlot mana;
        private WormholeSlot wormhole;

        private bool _cachedShowHealing;
        private bool _cachedShowMana;
        private bool _cachedShowWormhole;
        private int _cachedOffsetX;
        private int _cachedOffsetY;

        public override bool Visible => Main.playerInventory &&
            (PotionSlotsConfig.Instance.ShowHealingSlot ||
             PotionSlotsConfig.Instance.ShowManaSlot ||
             PotionSlotsConfig.Instance.ShowWormholeSlot);

        public override int InsertionIndex(List<GameInterfaceLayer> layers)
        {
            return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        }

        public override void OnInitialize()
        {
            var cfg = PotionSlotsConfig.Instance;
            _cachedShowHealing = cfg.ShowHealingSlot;
            _cachedShowMana = cfg.ShowManaSlot;
            _cachedShowWormhole = cfg.ShowWormholeSlot;
            _cachedOffsetX = cfg.OffsetX;
            _cachedOffsetY = cfg.OffsetY;

            life = new LifeSlot();
            mana = new ManaSlot();
            wormhole = new WormholeSlot();

            float left = 571 + cfg.OffsetX;
            float top = 105 + cfg.OffsetY;

            if (cfg.ShowHealingSlot) { SetSlotProperties(life, left, top); Append(life); top += 33; }
            if (cfg.ShowManaSlot)    { SetSlotProperties(mana, left, top); Append(mana); top += 33; }
            if (cfg.ShowWormholeSlot){ SetSlotProperties(wormhole, left, top); Append(wormhole); }
        }

        private void SetSlotProperties(UIElement slot, float left, float top)
        {
            slot.Width.Set(31, 0);
            slot.Height.Set(31, 0);
            slot.Left.Set(left, 0);
            slot.Top.Set(top, 0);
        }

        public override void SafeUpdate(GameTime gameTime)
        {
            var cfg = PotionSlotsConfig.Instance;
            if (cfg.ShowHealingSlot != _cachedShowHealing ||
                cfg.ShowManaSlot    != _cachedShowMana    ||
                cfg.ShowWormholeSlot!= _cachedShowWormhole||
                cfg.OffsetX         != _cachedOffsetX     ||
                cfg.OffsetY         != _cachedOffsetY)
            {
                RemoveAllChildren();
                OnInitialize();
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var cfg = PotionSlotsConfig.Instance;
            var font = Terraria.GameContent.FontAssets.MouseText.Value;
            spriteBatch.DrawString(font, "Potions", new Vector2(570 + cfg.OffsetX, 85 + cfg.OffsetY), Main.MouseTextColorReal, 0f, Vector2.Zero, 0.7f, 0, 0);

            base.Draw(spriteBatch);
        }
    }

    public abstract class PotionSlot : SmartUIElement
    {
        public abstract ref Item item { get; }

        public abstract Func<Item, bool> isValid { get; }

        public abstract string Texture { get; }

        public abstract string TextureFilled { get; }

        public bool NoItem => item is null || item.IsAir;

        public PotionSlot()
        {
            Width.Set(42, 0);
            Height.Set(42, 0);
        }

        public override void SafeClick(UIMouseEvent evt)
        {
            if (!Main.mouseItem.IsAir && isValid(Main.mouseItem) && NoItem)
            {
                item = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else if (Main.mouseItem.IsAir && !NoItem)
            {
                Main.mouseItem = item.Clone();
                item.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else if (!Main.mouseItem.IsAir && isValid(Main.mouseItem) && !NoItem)
            {
                if (Main.mouseItem.type != item.type)
                {
                    var temp = item.Clone();
                    item = Main.mouseItem.Clone();
                    Main.mouseItem = temp;
                }
                else
                {
                    var joined = item.stack + Main.mouseItem.stack;
                    if (joined > item.maxStack)
                    {
                        item.stack = item.maxStack;
                        Main.mouseItem.stack = joined - item.maxStack;
                    }
                    else
                    {
                        item.stack = joined;
                        Main.mouseItem.TurnToAir();
                    }
                }
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        public override void SafeRightClick(UIMouseEvent evt)
        {
            if (Main.mouseItem.IsAir && !NoItem)
            {
                var temp = item.Clone();
                temp.stack = 1;
                Main.mouseItem = temp;

                item.stack--;

                if (item.stack <= 0)
                    item.TurnToAir();

                SoundEngine.PlaySound(SoundID.MenuTick);
            }
            else if (!Main.mouseItem.IsAir && !NoItem && Main.mouseItem.type == item.type && Main.mouseItem.stack < Main.mouseItem.maxStack)
            {
                Main.mouseItem.stack++;
                item.stack--;

                if (item.stack <= 0)
                    item.TurnToAir();

                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var tex = ModContent.Request<Texture2D>(NoItem ? Texture : TextureFilled).Value;
            spriteBatch.Draw(tex, GetDimensions().ToRectangle(), Color.White);

            if (!NoItem)
            {
                Main.inventoryScale = 36 / 52f * 31 / 36f;
                ItemSlot.Draw(spriteBatch, ref item, 21, GetDimensions().Position());

                if (IsMouseHovering)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = "a";
                }
            }

            if (IsMouseHovering)
                Main.LocalPlayer.mouseInterface = true;
        }
    }

    public class LifeSlot : PotionSlot
    {
        public override ref Item item => ref Main.LocalPlayer.GetModPlayer<PotionStoragePlayer>().lifeSlot;

        public override Func<Item, bool> isValid => (item) => item.healLife > 0;

        public override string Texture => "PotionSlots/Assets/healing_sprite";
        public override string TextureFilled => "PotionSlots/Assets/healingbg";
    }

    public class ManaSlot : PotionSlot
    {
        public override ref Item item => ref Main.LocalPlayer.GetModPlayer<PotionStoragePlayer>().manaSlot;

        public override Func<Item, bool> isValid => (item) => item.healMana > 0;

        public override string Texture => "PotionSlots/Assets/mana_sprite";
        public override string TextureFilled => "PotionSlots/Assets/manabg";
    }

    public class WormholeSlot : PotionSlot
    {
        public override ref Item item => ref Main.LocalPlayer.GetModPlayer<PotionStoragePlayer>().wormholeSlot;

        public override Func<Item, bool> isValid => (item) => item.type == ItemID.WormholePotion || item.type == ItemID.RecallPotion;

        public override string Texture => "PotionSlots/Assets/wormhole_sprite";
        public override string TextureFilled => "PotionSlots/Assets/wormholebg";
    }
}
