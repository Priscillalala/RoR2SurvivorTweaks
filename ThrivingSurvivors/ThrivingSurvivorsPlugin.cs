global using Path = System.IO.Path;
global using Plugin = ThrivingSurvivors.ThrivingSurvivorsPlugin;
global using Content = ThrivingSurvivors.ThrivingSurvivorsContent;
global using Random = UnityEngine.Random;
global using RoR2BepInExPack.GameAssetPathsBetter;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RoR2BepInExPack;
using UnityEngine.AddressableAssets;
using UnityEngine;
using MaterialEditors;
using RoR2;
using HG.Reflection;
using ThrivingSurvivors.Merc;
using RoR2.ContentManagement;
using ThrivingSurvivors.MercAlt;

[assembly:SearchableAttribute.OptIn]

namespace ThrivingSurvivors;

// TheseSurvivorsAre_tweaked_geeked_peak_freaked_notTooWeak_sleak_unableToSpeak_notSoBleak
// Survivors_to_groove_with_move_with_win_with_lose_with_loop_with_gloop_with_risk_with_rain_with_loot_with_rush_with_obliterate_with_fall_in_love_with
[BepInPlugin(GUID, NAME, VERSION)]
public class ThrivingSurvivorsPlugin : BaseUnityPlugin
{
    public const string
        GUID = "groovesalad." + NAME,
        NAME = "ThrivingSurvivors",
        VERSION = "1.0.0";

    public static new ManualLogSource Logger { get; private set; }
    public static Harmony Harmony { get; private set; }
    public static string RuntimeDirectory { get; private set; }

    void Awake()
    {
        Logger = base.Logger;
        Harmony = new Harmony(GUID);
        RuntimeDirectory = Path.GetDirectoryName(Info.Location);

        ContentManager.collectContentPackProviders += add => add(new Content());

        Prefab.Init();
        LanguageLoader.Init();
        
        TweakMercAlt.Init();
    }
}