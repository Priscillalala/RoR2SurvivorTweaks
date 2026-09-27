using EntityStates;
using EntityStates.Merc;
using HarmonyLib;
using HG;
using HG.GeneralSerializer;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using ThrivingSurvivors.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ThrivingSurvivors.Documentation;

public static class Museum
{
    static readonly List<string> documentedSurvivorNames = [];

#if false
    public static void RequestSkillKeywords(params IEnumerable<string> skillDefKeys)
    {
        static void AddKeywordToSkill(SkillDef skillDef)
        {
            string keyword = $"{Plugin.NAME.ToUpperInvariant()}_";
            ArrayUtils.ArrayAppend(ref skillDef.keywordTokens, keyword);
        }

        Addressables.LoadAssetsAsync<SkillDef>(skillDefKeys, AddKeywordToSkill, Addressables.MergeMode.Union, false);
    }
#endif

    public static void RequestDocsForSurvivor(string survivorName)
    {
        documentedSurvivorNames.Add(survivorName);
    }

    [UsedImplicitly]
    [SystemInitializer(typeof(SurvivorCatalog), typeof(BodyCatalog))]
    static void SetupSurvivorDocumentation()
    {
        Plugin.Harmony.PatchAll(typeof(Museum));

        foreach (string survivorName in documentedSurvivorNames)
        {
            SurvivorIndex survivorIndex = SurvivorCatalog.FindSurvivorIndex(survivorName);
            if (survivorIndex == SurvivorIndex.None)
            {
                Plugin.Logger.LogWarning($"Requested docs for survivor {survivorName} who does not exist!");
                continue;
            }
            BodyIndex bodyIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorIndex);
            if (bodyIndex == BodyIndex.None)
            {
                Plugin.Logger.LogWarning($"Requested docs for survivor {survivorName} who has no body!");
                continue;
            }
            //string keywordTokenFormat = $"GROOVE_{survivorName.ToUpperInvariant()}_{{0}}_DIFF";
            var allSkills = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex)
                .SelectMany(x => x.skillFamily.variants)
                .Select(x => x.skillDef);
            foreach (SkillDef skillDef in allSkills)
            {
                //string keywordToken = string.Format(keywordTokenFormat, skillDef.skillName.ToUpperInvariant());
                string diffKeywordToken = skillDef.skillNameToken;
                if (diffKeywordToken.EndsWith("_NAME"))
                {
                    diffKeywordToken = diffKeywordToken[..^5];
                }
                diffKeywordToken = $"GROOVE_{diffKeywordToken}_DIFF";
                if (Language.english.TokenIsRegistered(diffKeywordToken))
                {
                    ArrayUtils.ArrayAppend(ref skillDef.keywordTokens, diffKeywordToken);
                }
            }
        }
    }

    [HarmonyILManipulator, HarmonyPatch(typeof(CharacterSelectController), nameof(CharacterSelectController.RebuildLocal))]
    static void AddSurvivorDiffToOverview(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locSurviorDef = -1;
        c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out locSurviorDef),
                x => x.MatchLdfld<SurvivorDef>(nameof(SurvivorDef.descriptionToken)),
                x => x.MatchCallOrCallvirt<Language>(nameof(Language.GetString))
                );
        c.Emit(OpCodes.Ldloc, locSurviorDef);
        c.EmitDelegate<Func<string, SurvivorDef, string>>((overviewText, survivorDef) =>
        {
            string survivorBaseToken = survivorDef.cachedName.ToUpperInvariant();

            string survivorDiffToken  = $"GROOVE_{survivorBaseToken}_DIFF";
            if (Language.english.TokenIsRegistered(survivorDiffToken))
            {
                overviewText = Language.GetString(survivorDiffToken) + "\n\n" + overviewText;
            }
            string survivorQuoteToken = $"GROOVE_{survivorBaseToken}_QUOTE";
            if (Language.english.TokenIsRegistered(survivorQuoteToken))
            {
                overviewText = Language.GetString(survivorQuoteToken) + "\n\n" + overviewText;
            }
            return overviewText;
        });
    }
}
