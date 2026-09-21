using HG;
using HG.GeneralSerializer;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
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
                string keywordToken = skillDef.skillNameToken;
                if (keywordToken.EndsWith("_NAME"))
                {
                    keywordToken = keywordToken[..^5];
                }
                keywordToken = $"GROOVE_{keywordToken}_DIFF";
                if (Language.english.TokenIsRegistered(keywordToken))
                {
                    ArrayUtils.ArrayAppend(ref skillDef.keywordTokens, keywordToken);
                }
            }
        }
    }
}
