using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Terraria.Audio;

namespace PotionSlots.Core
{
    internal class PotionStoragePlayer : ModPlayer
    {
        public Item lifeSlot;
        public Item manaSlot;
        public Item wormholeSlot;

        public override void Initialize()
        {
            lifeSlot = new Item();
            manaSlot = new Item();
            wormholeSlot = new Item();
        }

        public override void Load()
        {
            On_Player.QuickHeal_GetItemToUse += PickLifeSlot;
            On_Player.QuickMana_GetItemToUse += PickManaSlot;
            On_Player.GetItem += RouteItemToSlot;
        }

        private Item PickLifeSlot(On_Player.orig_QuickHeal_GetItemToUse orig, Player self)
        {
            var lifeSlot = self.GetModPlayer<PotionStoragePlayer>().lifeSlot;
            if (lifeSlot != null && !lifeSlot.IsAir)
                return lifeSlot;

            return orig(self);
        }

        private Item PickManaSlot(On_Player.orig_QuickMana_GetItemToUse orig, Player self)
        {
            var manaSlot = self.GetModPlayer<PotionStoragePlayer>().manaSlot;
            if (manaSlot != null && !manaSlot.IsAir)
                return manaSlot;

            return orig(self);
        }

        private static Item RouteItemToSlot(On_Player.orig_GetItem orig, Player self, int whoAmI, Item newItem, GetItemSettings settings)
        {
            var p = self.GetModPlayer<PotionStoragePlayer>();

            if (newItem.healLife > 0)
                TryFillSlot(ref p.lifeSlot, newItem);
            else if (newItem.healMana > 0)
                TryFillSlot(ref p.manaSlot, newItem);
            else if (newItem.type == ItemID.WormholePotion)
                TryFillSlot(ref p.wormholeSlot, newItem);

            return orig(self, whoAmI, newItem, settings);
        }

        private static void TryFillSlot(ref Item slot, Item incoming)
        {
            if (!slot.IsAir && slot.type == incoming.type && slot.stack < slot.maxStack)
            {
                int transferable = Math.Min(incoming.stack, slot.maxStack - slot.stack);
                slot.stack += transferable;
                incoming.stack -= transferable;

                if (incoming.stack <= 0)
                {
                    incoming.TurnToAir();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag.Add("life", lifeSlot);
            tag.Add("mana", manaSlot);
            tag.Add("wormhole", wormholeSlot);
        }

        public override void LoadData(TagCompound tag)
        {
            lifeSlot = tag.Get<Item>("life");
            manaSlot = tag.Get<Item>("mana");
            wormholeSlot = tag.Get<Item>("wormhole");
        }
    }
}
