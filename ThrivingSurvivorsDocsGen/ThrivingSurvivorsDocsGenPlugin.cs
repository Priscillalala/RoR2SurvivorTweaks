global using RoR2BepInExPack.GameAssetPathsBetter;
global using Path = System.IO.Path;
global using Plugin = ThrivingSurvivorsDocsGen.ThrivingSurvivorsDocsGenPlugin;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HG.Reflection;
using JetBrains.Annotations;
using RoR2;
using RoR2.Skills;
using RoR2BepInExPack;
using System.Text;
using ThrivingSurvivors.Documentation;
using UnityEngine;
using UnityEngine.AddressableAssets;

[assembly:SearchableAttribute.OptIn]

namespace ThrivingSurvivorsDocsGen;

[BepInPlugin(GUID, NAME, VERSION)]
public class ThrivingSurvivorsDocsGenPlugin : BaseUnityPlugin
{
    public const string
        GUID = "groovesalad." + NAME,
        NAME = "ThrivingSurvivorsDocsGen",
        VERSION = "1.0.0";

    public static new ManualLogSource Logger { get; private set; }
    public static Harmony Harmony { get; private set; }
    public static string RuntimeDirectory { get; private set; }

    void Awake()
    {
        Logger = base.Logger;
        Harmony = new Harmony(GUID);
        RuntimeDirectory = Path.GetDirectoryName(Info.Location);

    }

    [UsedImplicitly]
    [InitDuringStartupPhase(GameInitPhase.DuringIntro)]
    static void GenerateSurvivorDocumention()
    {
        StringBuilder sbFile = new();
        foreach (SurvivorDef survivorDef in SurvivorCatalog.orderedSurvivorDefs)
        {
            BodyIndex bodyIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorDef.survivorIndex);
            CharacterBody body = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);
            var allSkills = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex)
                .SelectMany(x => x.skillFamily.variants)
                .Select(x => x.skillDef);
            StringBuilder sbSkills = new();
            foreach (SkillDef skill in allSkills)
            {
                string diffToken = skill.keywordTokens.FirstOrDefault(x => x.StartsWith("GROOVE_") && x.EndsWith("_DIFF"));
                if (diffToken == null)
                {
                    continue;
                }
                string diffString = Language.english.GetLocalizedStringByToken(diffToken);
                string localizedSkillName = Language.english.GetLocalizedStringByToken(skill.skillNameToken);
                string skillIconUrl = $"https://riskofrain2.wiki.gg/images/{localizedSkillName.Replace(' ', '_')}.png";
                sbSkills.AppendLine($"### <img src=\"{skillIconUrl}\" width=\"24\"> {localizedSkillName}");
                sbSkills.AppendLine();
                diffString = diffString["<style=GrooveDiffKeyword>".Length..^"</style>".Length];
                var diffItemStrings = diffString
                    .Split(["<style=GrooveDiffItem>"], StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x[..^"</style>".Length]);
                foreach (string readonlyDiffItemString in diffItemStrings)
                {
                    string diffItemString = readonlyDiffItemString;
                    while (true)
                    {
                        int tagStartIndex = diffItemString.IndexOf('<');
                        int tagEndIndex = diffItemString.IndexOf('>');
                        if (tagEndIndex == -1 || tagEndIndex == -1)
                        {
                            break;
                        }
                        diffItemString = diffItemString.Remove(tagStartIndex, tagEndIndex - tagStartIndex + 1);
#if false
                        diffItemString = diffItemString.Insert(tagStartIndex, "**");
#endif
                    }
                    sbSkills.AppendLine($"- {diffItemString}");
                }
                sbSkills.AppendLine();
            }
            string skillsString = sbSkills.ToString();
            if (string.IsNullOrEmpty(skillsString))
            {
                continue;
            }
            string localizedSurvivorName = Language.english.GetLocalizedStringByToken(survivorDef.displayNameToken);
            string survivorIconUrl = $"https://riskofrain2.wiki.gg/images/{localizedSurvivorName.Replace(' ', '_')}.png";
            sbFile.AppendLine($"## <img src=\"{survivorIconUrl}\" width=\"32\"> {localizedSurvivorName}");
            sbFile.AppendLine();
            sbFile.AppendLine("<details>");
            sbFile.AppendLine("<summary>Click to see skill details</summary>");
            sbFile.AppendLine();
            sbFile.Append(skillsString);
            sbFile.AppendLine("</details>");
            sbFile.AppendLine();
        }

        string filePath = Path.Combine(RuntimeDirectory, "docs.md");
        File.WriteAllText(filePath, sbFile.ToString());
    }
}