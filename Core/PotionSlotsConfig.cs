using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace PotionSlotsUpdated.Core
{
    public class PotionSlotsConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public static PotionSlotsConfig Instance => ModContent.GetInstance<PotionSlotsConfig>();

        [DefaultValue(true)]
        public bool ShowHealingSlot { get; set; }

        [DefaultValue(true)]
        public bool ShowManaSlot { get; set; }

        [DefaultValue(true)]
        public bool ShowWormholeSlot { get; set; }

        [DefaultValue(false)]
        public bool WormholeSlotAsBuff { get; set; }

        [DefaultValue(false)]
        public bool AutoFillEmptySlots { get; set; }

        [DefaultValue(0)]
        [Range(-300, 300)]
        public int OffsetX { get; set; }

        [DefaultValue(0)]
        [Range(-300, 300)]
        public int OffsetY { get; set; }
    }
}
