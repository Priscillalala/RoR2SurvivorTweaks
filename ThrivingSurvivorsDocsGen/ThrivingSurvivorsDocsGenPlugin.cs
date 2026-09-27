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
            StringBuilder sbDetails = new();
            string survivorBaseToken = survivorDef.cachedName.ToUpperInvariant();

            string survivorDiffToken = $"GROOVE_{survivorBaseToken}_DIFF";
            if (Language.english.TokenIsRegistered(survivorDiffToken))
            {
                string survivorDiffString = Language.english.GetLocalizedStringByToken(survivorDiffToken);
                BuildDiffString(survivorDiffString, sbDetails);
            }
            foreach (SkillDef skill in allSkills)
            {
                string skillDiffToken = skill.keywordTokens.FirstOrDefault(x => x.StartsWith("GROOVE_") && x.EndsWith("_DIFF"));
                if (skillDiffToken == null)
                {
                    continue;
                }
                string skillDiffString = Language.english.GetLocalizedStringByToken(skillDiffToken);
                string localizedSkillName = Language.english.GetLocalizedStringByToken(skill.skillNameToken);
                string skillIconUrl = $"https://riskofrain2.wiki.gg/images/{localizedSkillName.Replace(' ', '_')}.png";
                sbDetails.AppendLine($"### <img src=\"{skillIconUrl}\" width=\"24\"> {localizedSkillName}");
                sbDetails.AppendLine();
                BuildDiffString(skillDiffString, sbDetails);
            }
            string detailsString = sbDetails.ToString();
            if (string.IsNullOrEmpty(detailsString))
            {
                continue;
            }
            string localizedSurvivorName = Language.english.GetLocalizedStringByToken(survivorDef.displayNameToken);
            string survivorIconUrl = $"https://riskofrain2.wiki.gg/images/{localizedSurvivorName.Replace(' ', '_')}.png";
            sbFile.AppendLine($"## <img src=\"{survivorIconUrl}\" width=\"32\"> {localizedSurvivorName}");
            sbFile.AppendLine();
            string survivorQuoteToken = $"GROOVE_{survivorBaseToken}_QUOTE";
            if (Language.english.TokenIsRegistered(survivorQuoteToken))
            {
                string survivorQuoteString = Language.english.GetLocalizedStringByToken(survivorQuoteToken);
                survivorQuoteString = survivorQuoteString["<style=GrooveQuote>".Length..^"</style>".Length];
                sbFile.AppendLine($"> {survivorQuoteString}");
                sbFile.AppendLine();
            }
            sbFile.AppendLine("<details>");
            sbFile.AppendLine("<summary>Expand details..</summary>");
            sbFile.AppendLine();
            sbFile.Append(detailsString);
            sbFile.AppendLine("</details>");
            sbFile.AppendLine();
        }

        static void BuildDiffString(string diffString, StringBuilder sb)
        {
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
                sb.AppendLine($"- {diffItemString}");
            }
            sb.AppendLine();
        }

        string filePath = Path.Combine(RuntimeDirectory, "docs.md");
        File.WriteAllText(filePath, sbFile.ToString());
    }
}