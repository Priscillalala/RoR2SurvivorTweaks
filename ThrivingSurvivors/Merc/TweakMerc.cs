using BepInEx;
using EntityStates;
using EntityStates.Merc;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using ThrivingSurvivors.Core;
using ThrivingSurvivors.Core.Components;
using ThrivingSurvivors.Documentation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ThrivingSurvivors.Merc;

public static class TweakMerc
{
    private static readonly Dictionary<Evis, HurtBox> evisTargetStorage = [];

    public static void Init()
    {
        LanguageLoader.RequestLanguageFile("MercOverrides.json");
        LanguageLoader.RequestLanguageFile("MercDocs.json");
        Museum.RequestDocsForSurvivor("Merc");

        Plugin.Harmony.PatchAll(typeof(TweakMerc));
        Plugin.Harmony.PatchAll(typeof(FocusedAssaultPatch));

        #region primary
        new ModifyEntityStateAsync(RoR2_Base_Merc.EntityStates_Merc_Weapon_GroundLight2_asset)
        {
            [nameof(BasicMeleeAttack.shorthopVelocityFromHit)] = -2f, // downwards velocity is capped to this value after hitpause
        };
        #endregion

        #region secondary
        const float SECONDARY_CD = 3;
        Helpers.ModifyGameAssetAsync<SkillDef>(RoR2_Base_Merc.MercBodyWhirlwind_asset, skillDef =>
        {
            skillDef.baseRechargeInterval = SECONDARY_CD;
        });
        Helpers.ModifyGameAssetAsync<SkillDef>(RoR2_Base_Merc.MercBodyUppercut_asset, skillDef =>
        {
            skillDef.baseRechargeInterval = SECONDARY_CD;
        });
        static void SetWhirlwindDamage(string key) => new ModifyEntityStateAsync(key)
        {
            [nameof(WhirlwindBase.baseDamageCoefficient)] = 2.3f,
        };
        SetWhirlwindDamage(RoR2_Base_Merc.EntityStates_Merc_WhirlwindBase_asset); // prob does nothing
        SetWhirlwindDamage(RoR2_Base_Merc.EntityStates_Merc_WhirlwindGround_asset);
        SetWhirlwindDamage(RoR2_Base_Merc.EntityStates_Merc_WhirlwindAir_asset);
        #endregion

        #region utility
        new ModifyEntityStateAsync(RoR2_Base_Merc.EntityStates_Merc_FocusedAssaultDash_asset)
        {
            //[nameof(FocusedAssaultDash.delayPerHit)] = .25f,
            [nameof(FocusedAssaultDash.delayedDamageCoefficient)] = 8f,
        };
        #endregion

        #region special
        const float SPECIAL_CD = 7;
        Helpers.ModifyGameAssetAsync<SkillDef>(RoR2_Base_Merc.MercBodyEvis_asset, skillDef =>
        {
            skillDef.baseRechargeInterval = SPECIAL_CD;
        });
        Helpers.ModifyGameAssetAsync<SkillDef>(RoR2_Base_Merc.MercBodyEvisProjectile_asset, skillDef =>
        {
            skillDef.baseRechargeInterval = SPECIAL_CD;
        });
        new ModifyEntityStateAsync(RoR2_Base_Merc.EntityStates_Merc_Evis_asset)
        {
            [nameof(Evis.duration)] = 1.15f,
            [nameof(Evis.lingeringInvincibilityDuration)] = 0.4f,
        };
        Helpers.ModifyGameAssetAsync<GameObject>(RoR2_Base_Merc.EvisOverlapProjectile_prefab, EvisOverlapProjectile =>
        {
            EvisOverlapProjectile.AddComponent<ProjectileOverlapUseAttackSpeed>();
            EvisOverlapProjectile.RemoveComponentImmediate<StartEvent>(); // sets expose damage type after a delay
            new ModifyComponent<ProjectileOverlapAttack>(EvisOverlapProjectile)
            {
                c = { fireFrequency = 6f, resetInterval = 1f / 6f }
            };
            new ModifyComponent<ProjectileSimple>(EvisOverlapProjectile)
            {
                c = { lifetime = 5f } // ProjectileFinalOverlapHit destroys before this
            };
            new AddComponent<ProjectileFinalOverlapHit>(EvisOverlapProjectile)
            {
                c = { finalHitDamageType = DamageType.ApplyMercExpose, lifetime = 1f }
            };
        });
        Helpers.ModifyGameAssetAsync<GameObject>(RoR2_Base_Merc.ImpactMercEvis_prefab, ImpactMercEvis =>
        {
            new AddComponent<RandomlyOffsetRotation>(ImpactMercEvis)
            {
                c = { maxAngle = 80f }
            };
        });
        #endregion
    }

    [HarmonyILManipulator, HarmonyPatch(typeof(EvisDash), nameof(EvisDash.FixedUpdate))]
    static void MakeEvisIgnoreAllies(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(HGPhysics), nameof(HGPhysics.OverlapSphere)));
        int locHurtBox = -1;
        ILLabel falseLabel = null;
        c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out locHurtBox),
                x => x.MatchLdfld<HurtBox>(nameof(HurtBox.healthComponent)),
                x => x.MatchLdarg(0),
                x => x.MatchGetProperty<EntityState>(nameof(EntityState.healthComponent)),
                x => x.MatchCallOrCallvirt<Object>("op_Inequality"),
                x => x.MatchBrfalse(out falseLabel)
                );
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, locHurtBox);
        c.EmitDelegate<Func<EvisDash, HurtBox, bool>>((evisState, targetHurtBox) =>
        {
            TeamMask unprotectedTeams = TeamMask.GetUnprotectedTeams(evisState.GetTeam());
            return unprotectedTeams.HasTeam(targetHurtBox.teamIndex);
        });
        c.Emit(OpCodes.Brfalse_S, falseLabel);
    }

    static HurtBox NewEvisTargetSearch(Evis evisState)
    {
        BullseyeSearch bullseyeSearch = new BullseyeSearch
        {
            searchOrigin = evisState.transform.position,
            searchDirection = Random.onUnitSphere,
            maxDistanceFilter = Evis.maxRadius,
            teamMaskFilter = TeamMask.GetUnprotectedTeams(evisState.GetTeam()),
            sortMode = BullseyeSearch.SortMode.Distance
        };
        bullseyeSearch.RefreshCandidates();
        bullseyeSearch.FilterOutGameObject(evisState.gameObject);
        HurtBox[] results = bullseyeSearch.GetResults().ToArray();
        foreach (HurtBox target in results)
        {
            if (target.healthComponent && target.healthComponent.body && target.healthComponent.body.HasBuff(RoR2Content.Buffs.MercExpose))
            {
                return target;
            }
        }
        return results.FirstOrDefault();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Evis), nameof(Evis.SearchForTarget))]
    static bool ModifyEvisTargetSearch(Evis __instance, ref HurtBox __result)
    {
        if (!evisTargetStorage.TryGetValue(__instance, out HurtBox currentTarget) || !currentTarget || !currentTarget.healthComponent || !currentTarget.healthComponent.alive)
        {
            currentTarget = NewEvisTargetSearch(__instance);
            evisTargetStorage[__instance] = currentTarget;
        }
        __result = currentTarget;
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Evis), nameof(Evis.OnExit))]
    static void CleanEvisTargetStorage(Evis __instance)
    {
        evisTargetStorage.Remove(__instance);
    }
}
