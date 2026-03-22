using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using RiskOfOptions;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using BepInEx.Configuration;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;



using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using Newtonsoft.Json.Utilities;



namespace BenthicRework
{
    [BepInDependency("com.rune580.riskofoptions")]

    // Soft Dependencies
    //Item Qualities Mod
    [BepInDependency("com.Gorakh.ItemQualities", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class BenthicRework : BaseUnityPlugin
    {

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Braquen";
        public const string PluginName = "Benthic_Rebloomed";
        public const string PluginVersion = "1.0.0";


        public static BepInEx.PluginInfo pluginInfo;
        public static AssetBundle AssetBundle;

        public static ConfigEntry<int> StacksUpgraded;
        public static ConfigEntry<int> ItemsAdded;
        public static ConfigEntry<bool> UpgradeLunars;
        public static ConfigEntry<bool> UpgradeSelf;

        // public static ConfigEntry<ItemTier[]> CustomBadTiers;
        // public static ConfigEntry<ItemIndex[]> CustomBadItems;

        public void Awake()
        {
            Log.Init(Logger);

            pluginInfo = Info;

            var Config = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, "braquen-BenthicRebloomed.cfg"), true);

            StacksUpgraded = Config.Bind("Benthic Stats", "Stacks Upgraded", 3, "Number of stacks each bloom upgrades on stage advance. Setting 0 restores original benthic bloom functionality.");
            ItemsAdded = Config.Bind("Benthic Stats", "Items Granted", 2, "Number of items added to a stack when upgraded. Setting 0 restores original benthic bloom functionality.");

            UpgradeLunars = Config.Bind("Allowed Upgrades", "Allow Lunar Upgrades", false, "When enabled, bloom can grant more stacks of lunar items on stage advance.");
            UpgradeSelf = Config.Bind("Allowed Upgrades", "Allow Self Upgrade", false, "When enabled, bloom can grant more stacks of itself on stage advance. Completely unbalanced.");

            // CustomBadTiers = Config.Bind<ItemTier[]>("Allowed Upgrades", "Tier Blacklist", [], "Other Item Tiers Benthic should not upgrade.");
            // CustomBadItems = Config.Bind<ItemIndex[]>("Allowed Upgrades", "Item Blacklists", [], "Other Items Benthic should not upgrade.");

            //Set the max to 100, because only god can judge.
            ModSettingsManager.AddOption(new IntSliderOption(StacksUpgraded,
                new IntSliderConfig { min = 0, max = 100 }));
            ModSettingsManager.AddOption(new IntSliderOption(ItemsAdded,
                new IntSliderConfig { min = 1, max = 100 }));
            ModSettingsManager.AddOption(new CheckBoxOption(UpgradeLunars));
            ModSettingsManager.AddOption(new CheckBoxOption(UpgradeSelf));


            ModSettingsManager.SetModDescription("Braq's Big Beautiful Benthic Bloom Betterer");
            var sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/CloverVoid/texCloverVoidIcon.png").WaitForCompletion();
            if(sprite != null) ModSettingsManager.SetModIcon(sprite);

            UpdateText();

            //StacksUpgraded.SettingChanged += (obj, args) => UpdateText();
            //ItemsAdded.SettingChanged += (obj, args) => UpdateText();

            IL.RoR2.CharacterMaster.TryCloverVoidUpgrades += CharacterMaster_TryCloverVoidUpgrades;
        }

        private void CharacterMaster_TryCloverVoidUpgrades(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            Log.Debug("Inserting alternate Benthic Bloom functionality");
            try
            {
                c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(2),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CharacterMaster>("cloverVoidRng")
                );

                c.Index += 1;
                //itemcount is on the stack
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_2);
                c.EmitDelegate(AlternateBenthic);
                //if mod is off, returns the supplied item count and nothing changes. If on, returns 0 and the rest of the code isn't run.

            }
            catch (Exception e) { ErrorHookFailed("Add alternate Benthic Functionality", e); }
        }

        private static int AlternateBenthic(int itemCount, CharacterMaster characterMaster, List<ItemIndex> itemsCollected)
        {
            int num = StacksUpgraded.Value;
            int num2 = ItemsAdded.Value;
            if (num == 0 || num2 == 0) return itemCount;

            bool upSelf = UpgradeSelf.Value;
            bool upLunar = UpgradeLunars.Value;

            int listCount = itemsCollected.Count;
            int upgrades = itemCount * num;

            bool[] wasUpgraded = new bool[listCount];

            int i = 0;
            int j = 0;
            while (i < upgrades)
            {
                if (j >= listCount)
                {
                    if (i == 0)
                    {
                        //no upgradeable items found
                        break;
                    }

                    //back to the first item in the list
                    j = 0;
                    continue;
                }

                ItemIndex item = itemsCollected[j];

                ItemTier[] badTiers = [ItemTier.Lunar, ItemTier.NoTier];

                if ((item == DLC1Content.Items.CloverVoid.itemIndex && !upSelf) ||
                    ItemCatalog.GetItemDef(item).tier == ItemTier.Lunar && !upLunar ||
                    ItemCatalog.GetItemDef(item).tier == ItemTier.NoTier)
                {
                    //skip this item
                    j++;
                    continue;
                }
                // if(Array.IndexOf(CustomBadItems.Value, item) >= 0 ||
                //    Array.IndexOf(CustomBadTiers.Value, ItemCatalog.GetItemDef(item).tier) >= 0)
                // {
                //     //skip this item
                //     j++;
                //     continue;
                // }
                

                // compat.QualityModCompatibility.BenthicQualityEffectHandler(characterMaster, )

                wasUpgraded[j] = true;
                characterMaster.inventory.GiveItemPermanent(item, num2);

                i++;
                j++;
            }

            for (int k = 0; k < listCount; k++)
            {
                if (wasUpgraded[k]) CharacterMasterNotificationQueue.PushItemNotification(characterMaster, itemsCollected[k]);
            }

            if (i > 0 && characterMaster.bodyInstanceObject)
            {
                Util.PlaySound("Play_item_proc_extraLife", characterMaster.bodyInstanceObject);
            }

            //prevents the loop from running, thus negating the rest of the code.
            return 0;
        }

        internal static void ErrorHookFailed(string name, Exception e)
        {
            Log.Error(name + " hook failed: " + e.Message);
        }

        private void UpdateText()
        {
            if(ItemsAdded.Value == 0 || StacksUpgraded.Value == 0) { return; }

            string token = "ITEM_CLOVERVOID_DESC";
            string text = "<style=cIsUtility>Adds " + ItemsAdded.Value +" </style> items to <style=cIsUtility>" + StacksUpgraded.Value + "</style> <style=cStack>(+" + StacksUpgraded.Value + " per stack)</style> item stacks at the <style=cIsUtility>start of each stage</style>. <style=cIsVoid>Corrupts all 57 Leaf Clovers</style>.";
            string token2 = "ITEM_CLOVERVOID_PICKUP";
            string text2 = "Upgrades your item stacks at the start of each stage.";
            ReplaceString(token, text);
            ReplaceString(token2, text2);
        }

        private void ReplaceString(string token, string newtext)
        {
            LanguageAPI.Add(token, newtext);
        }
    }
}

