using HarmonyLib;
using RoR2;
using ThrivingSurvivors.Core;
using TMPro;

namespace ThrivingSurvivors;

public static class LanguageLoader
{
    static string languageRootFolder;
    static string languageDocsRootFolder;
    static readonly List<string> requestedLanguageFileNames = [];
    static readonly List<string> requestedLanguageDocsFileNames = [];

    public static void Init()
    {
        languageRootFolder = Path.Combine(Plugin.RuntimeDirectory, "Language");
        languageDocsRootFolder = Path.Combine(Plugin.RuntimeDirectory, "LanguageDocs");
        Plugin.Harmony.PatchAll(typeof(LanguageLoader));

        Helpers.ModifyGameAssetAsync<TMP_StyleSheet>(TextMesh_Pro_FormerResources.TMP_Default_Style_Sheet_asset, defaultStyleSheet =>
        {
            // white color overrides the <style=cSub> rollover from poorly formatted keywords above
            defaultStyleSheet.styles.Add(new("GrooveDiffKeyword", "<color=white><b>  <sprite name=\"CloudLeft\"> CHANGELOG.md <sprite name=\"CloudRight\">  </b></color><style=cSub>", "</style>"));
            const string DIFF_ITEM_OPEN = "\n☂<indent=1.5em>";
            const string DIFF_ITEM_CLOSE = "</indent>";
            defaultStyleSheet.styles.Add(new("GrooveDiffItem", DIFF_ITEM_OPEN, DIFF_ITEM_CLOSE));
            defaultStyleSheet.styles.Add(new("GrooveQuote", "<i>", "</i>"));
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

    public static void RequestLanguageDocsFile(string fileName)
    {
        requestedLanguageDocsFileNames.Add(fileName);
    }

    // Same as using Language.collectLanguageRootFolders, but we need to add our overrides after the ror2 strings
    [HarmonyPostfix, HarmonyPatch(typeof(Language), nameof(Language.GetLanguageRootFolders))]
    static void AddOverrideLanguageRootFolders(List<string> __result)
    {
        __result.Add(languageRootFolder);
        __result.Add(languageDocsRootFolder); // docs don't need to be overrides but we might as well add them here too
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Language), nameof(Language.LoadAllTokensFromFolder))]
    static bool LoadRequestedLanguageFiles(string folder, List<KeyValuePair<string, string>> output)
    {
        void LoadTokensFromRequestedLanguageFiles(List<string> requestedLanguageFileNames)
        {
            foreach (string requestedFileName in requestedLanguageFileNames)
            {
                string requestedFilePath = Path.Combine(folder, requestedFileName);
                if (File.Exists(requestedFilePath))
                {
                    Language.LoadTokensFromData(File.ReadAllText(requestedFilePath), output);
                }
            }
        }
        string rootFolder = Path.GetDirectoryName(folder);
        if (rootFolder == languageRootFolder)
        {
            LoadTokensFromRequestedLanguageFiles(requestedLanguageFileNames);
            return false;
        }
        if (rootFolder == languageDocsRootFolder)
        {
            LoadTokensFromRequestedLanguageFiles(requestedLanguageDocsFileNames);
            return false;
        }
        return true;
    }
}