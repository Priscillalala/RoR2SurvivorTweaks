global using RoR2BepInExPack.GameAssetPathsBetter;
global using Content = ThrivingSurvivors.ThrivingSurvivorsContent;
global using Path = System.IO.Path;
global using Plugin = ThrivingSurvivors.ThrivingSurvivorsPlugin;
global using Random = UnityEngine.Random;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HG.Reflection;
using RoR2.ContentManagement;
using ThrivingSurvivors.Merc;

[assembly: SearchableAttribute.OptIn]

namespace ThrivingSurvivors;

[BepInPlugin(GUID, NAME, VERSION)]
public class ThrivingSurvivorsPlugin : BaseUnityPlugin
{
    public const string
        GUID = "groovesalad." + NAME,
        NAME = "ThrivingSurvivors",
        VERSION = "0.1.0";

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
        
        TweakMerc.Init();
    }
}