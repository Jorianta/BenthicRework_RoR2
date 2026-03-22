
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using RoR2;



namespace BenthicRework.compat
{
    public static class QualityModCompatibility
    {
        private static bool? _enabled;

        public static bool enabled {
            get {
                if (_enabled == null) {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Gorakh.ItemQualities");
                }
                return (bool)_enabled;
            }
        }

        public static bool BenthicQualityEffectHandler(CharacterMaster master, Inventory.ItemTransformation itemTransformation, List<ItemIndex> upgradableItems)
        {
            if(enabled) _BenthicQualityEffectHandler(master, itemTransformation, upgradableItems);
            return enabled;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void _BenthicQualityEffectHandler(CharacterMaster master, Inventory.ItemTransformation itemTransformation, List<ItemIndex> upgradableItems)
        {
            // ItemQualities.Items.CloverVoid.upgradeItemQualities();
            
        
        }
    }
}
