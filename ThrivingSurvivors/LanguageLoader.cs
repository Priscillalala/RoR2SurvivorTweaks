using HarmonyLib;
using RoR2;
using ThrivingSurvivors.Core;
using TMPro;

public static class LanguageLoader
{
    static string languageRootFolder;
    static readonly HashSet<string> requestedLanguageFileNames = [];

    public static void Init()
    {
        languageRootFolder = Path.Combine(Plugin.RuntimeDirectory, "Language");
        Plugin.Harmony.PatchAll(typeof(LanguageLoader));

        Helpers.ModifyGameAssetAsync<TMP_StyleSheet>(TextMesh_Pro_FormerResources.TMP_Default_Style_Sheet_asset, defaultStyleSheet =>
        {
            // white color overrides the <style=cSub> rollover from poorly formatted keywords above
            defaultStyleSheet.styles.Add(new("GrooveDiffKeyword", "<color=white><b>  <sprite name=\"CloudLeft\"> CHANGELOG.md <sprite name=\"CloudRight\">  </b></color><style=cSub>", "</style>"));
            const string DIFF_ITEM_OPEN = "\n☂<indent=1.5em>";
            const string DIFF_ITEM_CLOSE = "</indent>";
            defaultStyleSheet.styles.Add(new("GrooveDiffItem", DIFF_ITEM_OPEN, DIFF_ITEM_CLOSE));
#if false
            //defaultStyleSheet.styles.Add(new("GrooveDiffItem", "\n☂<indent=1.5em>", "</indent>"));

            //defaultStyleSheet.styles.Add(new("GrooveDiffBuff", "\n<mspace=.5em><+></mspace><indent=2.5em>", "</indent>"));
            //defaultStyleSheet.styles.Add(new("GrooveDiffNerf", "\n<mspace=.5em><-></mspace><indent=2.5em>", "</indent>"));
#endif
        });
    }

    public static void RequestLanguageFile(string fileName)
    {
        requestedLanguageFileNames.Add(fileName);
    }

    // Same as using Language.collectLanguageRootFolders, but we need to add our overrides after the ror2 strings
    [HarmonyPostfix, HarmonyPatch(typeof(Language), nameof(Language.GetLanguageRootFolders))]
    static void AddOverrideLanguageRootFolders(List<string> __result)
    {
        __result.Add(languageRootFolder);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Language), nameof(Language.LoadAllTokensFromFolder))]
    static bool LoadRequestedLanguageFiles(string folder, List<KeyValuePair<string, string>> output)
    {
        string rootFolder = Path.GetDirectoryName(folder);
        if (rootFolder != languageRootFolder)
        {
            return true;
        }
        foreach (string filePath in Directory.EnumerateFiles(folder))
        {
            string fileName = Path.GetFileName(filePath);
            if (requestedLanguageFileNames.Contains(fileName))
            {
                Language.LoadTokensFromData(File.ReadAllText(filePath), output);
            }
        }
        return false;
    }
}