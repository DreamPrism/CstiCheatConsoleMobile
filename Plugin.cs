using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CstiCheatConsoleMobile.Plugin), "CstiCheatConsoleMobile", "0.0.1", "DreamPrism")]
[assembly: MelonGame("WinterSpring Games", "Card Survival - Tropical Island")]
[assembly: MelonGame("WinterSpringGames", "CardSurvivalTropicalIsland")]
[assembly: MelonGame("winterspringgames", "survivaljourney")]
[assembly: MelonGame("winterspringgames", "survivaljourneydemo")]
[assembly: HarmonyDontPatchAll]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.ANDROID)]

namespace CstiCheatConsoleMobile
{
    public class Plugin : MelonMod
    {
        public static bool Enabled { get; private set; }
        public static bool CombatInvincible { get; set; }
        public static bool FastExploration { get; set; }
        
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Csti Cheat Mode loading...");
            Enabled = true;
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Patches));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Localization));
            LoggerInstance.Msg("Csti Cheat Mode loaded!");
        }
    }
}