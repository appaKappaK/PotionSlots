using Terraria;
using Terraria.ModLoader;

namespace PotionSlotsUpdated.Core
{
    // LuiAFK Reborn compatibility: count slot items toward infinite potion thresholds.
    // LuiAFK Reborn's ConsumeItem only checks player.inventory[], so potions stored in our
    // custom slots don't count. This GlobalItem adds our slot stacks to the total so the
    // infinite effect triggers correctly (thresholds match LuiAFK Reborn 1.2.5).
    internal class PotionSlotsGlobalItem : GlobalItem
    {
        public override bool ConsumeItem(Item item, Player player)
        {
            if (!ModLoader.HasMod("miningcracks_take_on_luiafk"))
                return true;

            var p = player.GetModPlayer<PotionStoragePlayer>();
            int slotCount = 0;

            if (item.healLife > 0 && p.lifeSlot.type == item.type)
                slotCount = p.lifeSlot.stack;
            else if (item.healMana > 0 && p.manaSlot.type == item.type)
                slotCount = p.manaSlot.stack;
            else if (item.buffType > 0 && p.wormholeSlot.type == item.type)
                slotCount = p.wormholeSlot.stack;

            if (slotCount == 0)
                return true;

            int inventoryCount = 0;
            foreach (var inv in player.inventory)
                if (inv.type == item.type)
                    inventoryCount += inv.stack;

            int total = slotCount + inventoryCount;

            if (item.healLife > 0 && total >= 90) return false;
            if (item.healMana > 0 && total >= 225) return false;
            if (item.buffType > 0 && total >= 30) return false;

            return true;
        }
    }
}
