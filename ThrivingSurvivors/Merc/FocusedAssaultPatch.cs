using EntityStates.Merc;
using HarmonyLib;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.Orbs;
using UnityEngine;

namespace ThrivingSurvivors.Merc;

public static class FocusedAssaultPatch
{
    private static readonly Dictionary<UnityObjectWrapperKey<GameObject>, List<DelayedHitOrb>> focusedAssaultOrbsStorage = [];
    #if INTERRUPT_FOCUSED_ASSAULT
    private static readonly HashSet<FocusedAssaultDash> focusedAssaultBufferedSecondary = [];
#endif

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
    static void ExitFocusedAssaultDashAndSpreadOrbDamage(FocusedAssaultDash __instance)
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
        #if INTERRUPT_FOCUSED_ASSAULT
        focusedAssaultBufferedSecondary.Remove(__instance);
#endif
    }

#if INTERRUPT_FOCUSED_ASSAULT
    [HarmonyPostfix, HarmonyPatch(typeof(FocusedAssaultDash), nameof(FocusedAssaultDash.AuthorityFixedUpdate))]
    static void BufferSecondary(FocusedAssaultDash __instance)
    {
        bool isSecondaryBuffered = focusedAssaultBufferedSecondary.Contains(__instance);
        if (isSecondaryBuffered)
        {
            if (!__instance.authorityInHitPause)
            {
                __instance.skillLocator.secondary.ExecuteIfReady();
                focusedAssaultBufferedSecondary.Remove(__instance);
            }
        }
        else if (__instance.skillLocator && __instance.skillLocator.secondary.IsReady() && __instance.inputBank.skill2.down)
        {
            focusedAssaultBufferedSecondary.Add(__instance);
        }
    }
#endif
}
