using BepInEx;
using EntityStates;
using EntityStates.Merc;
using HarmonyLib;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using ThrivingSurvivors.Core;
using ThrivingSurvivors.Core.Components;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ThrivingSurvivors.MercAlt;

public static class FocusedAssaultPatch
{
    private static readonly Dictionary<UnityObjectWrapperKey<GameObject>, List<DelayedHitOrb>> focusedAssaultOrbsStorage = [];

    [HarmonyILManipulator, HarmonyPatch(typeof(FocusedAssaultDash), nameof(FocusedAssaultDash.HandleHit))]
    static void TrackOrbs(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<OrbManager>(nameof(OrbManager.AddOrb))
                );
        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Action<DelayedHitOrb, GameObject>>((orb, attacker) =>
        {
            if (!focusedAssaultOrbsStorage.TryGetValue(attacker, out var orbsList))
            {
                focusedAssaultOrbsStorage.Add(attacker, orbsList = []);
            }
            orbsList.Add(orb);
        });
    }

    [HarmonyPostfix, HarmonyPatch(typeof(FocusedAssaultDash), nameof(FocusedAssaultDash.OnExit))]
    static void SpreadOrbDamage(FocusedAssaultDash __instance)
    {
        var attacker = __instance.gameObject;
        if (focusedAssaultOrbsStorage.TryGetValue(attacker, out var orbsList))
        {
            focusedAssaultOrbsStorage.Remove(attacker);
            float damageMult = 1f / orbsList.Count;
            foreach (DelayedHitOrb orb in orbsList)
            {
                orb.damageValue *= damageMult;
            }
        }
    }

#if false
    [HarmonyILManipulator, HarmonyPatch(typeof(FocusedAssaultDash), nameof(FocusedAssaultDash.OnMeleeHitAuthority))]
    static void FocusedAssaultDamageReduction(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<FocusedAssaultDash>(nameof(FocusedAssaultDash.delayedDamageCoefficient)),
                x => x.MatchMul()
                );
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<float, FocusedAssaultDash, float>>((damageValue, state) =>
        {
            return damageValue * Mathf.Pow(.85f, state.currentHitCount - 1);
        });
    }
#endif
}
