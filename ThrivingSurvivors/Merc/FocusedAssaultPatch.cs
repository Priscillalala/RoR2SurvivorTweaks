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
}
