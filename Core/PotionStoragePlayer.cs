using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Terraria.Audio;

namespace PotionSlotsUpdated.Core
{
    internal class PotionStoragePlayer : ModPlayer
    {
        public Item lifeSlot;
        public Item manaSlot;
        public Item wormholeSlot;

        private static Hook _qwkHas30Hook;
        private static Hook _qwkHasOneHook;

        private delegate bool Del_Has30WormholePotion(object self, Player player, int requiredAmount);
        private delegate bool Del_HasWormholePotion(object self, Player player);

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
            On_Player.QuickBuff += UseWormholeSlotOnQuickBuff;
            On_Player.GetItem += RouteItemToSlot;

            // QuickWormholeKeyBind compatibility: hook its inventory search to also check our slot
            if (ModLoader.TryGetMod("QuickWormholeKeyBind", out Mod qwkMod))
            {
                var wormholeBindType = qwkMod.GetType().Assembly.GetType("QuickWormholeKeyBind.WormholeBind");
                _qwkHas30Hook = new Hook(
                    wormholeBindType.GetMethod("Has30WormholePotion", BindingFlags.NonPublic | BindingFlags.Instance),
                    (Del_Has30WormholePotion orig, object self, Player player, int requiredAmount) =>
                        orig(self, player, requiredAmount) || QWK_CheckSlotHas30(player, requiredAmount)
                );
                _qwkHasOneHook = new Hook(
                    wormholeBindType.GetMethod("HasWormholePotion", BindingFlags.NonPublic | BindingFlags.Instance),
                    (Del_HasWormholePotion orig, object self, Player player) =>
                        orig(self, player) || QWK_ConsumeSlotPotion(player)
                );
            }
        }

        public override void Unload()
        {
            _qwkHas30Hook?.Dispose();
            _qwkHas30Hook = null;
            _qwkHasOneHook?.Dispose();
            _qwkHasOneHook = null;
        }

        private static bool QWK_CheckSlotHas30(Player player, int requiredAmount)
        {
            var p = player.GetModPlayer<PotionStoragePlayer>();
            return !PotionSlotsConfig.Instance.WormholeSlotAsBuff
                && p.wormholeSlot.type == ItemID.WormholePotion
                && p.wormholeSlot.stack >= requiredAmount;
        }

        private static bool QWK_ConsumeSlotPotion(Player player)
        {
            var p = player.GetModPlayer<PotionStoragePlayer>();
            if (!PotionSlotsConfig.Instance.WormholeSlotAsBuff
                && p.wormholeSlot.type == ItemID.WormholePotion
                && p.wormholeSlot.stack > 0)
            {
                p.wormholeSlot.stack--;
                if (p.wormholeSlot.stack <= 0)
                    p.wormholeSlot.TurnToAir();
                return true;
            }
            return false;
        }

        private static void UseWormholeSlotOnQuickBuff(On_Player.orig_QuickBuff orig, Player self)
        {
            orig(self);

            if (!PotionSlotsConfig.Instance.WormholeSlotAsBuff) return;

            var p = self.GetModPlayer<PotionStoragePlayer>();
            var item = p.wormholeSlot;

            if (item.IsAir || item.buffType <= 0 || !item.consumable) return;
            if (self.HasBuff(item.buffType)) return;

            self.AddBuff(item.buffType, item.buffTime);
            item.stack--;
            if (item.stack <= 0)
                item.TurnToAir();
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
            else if (PotionSlotsConfig.Instance.WormholeSlotAsBuff
                     ? newItem.consumable && newItem.buffType > 0 && newItem.healLife <= 0 && newItem.healMana <= 0
                     : newItem.type == ItemID.WormholePotion || newItem.type == ItemID.RecallPotion)
                TryFillSlot(ref p.wormholeSlot, newItem);

            return orig(self, whoAmI, newItem, settings);
        }

        private static void TryFillSlot(ref Item slot, Item incoming)
        {
            if (slot.IsAir)
            {
                if (PotionSlotsConfig.Instance.AutoFillEmptySlots)
                {
                    slot = incoming.Clone();
                    incoming.TurnToAir();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                return;
            }

            if (slot.type == incoming.type)
            {
                slot.stack += incoming.stack;
                incoming.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
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
