using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ThrivingSurvivors.Core.Components;

[RequireComponent(typeof(ProjectileDamage))]
[RequireComponent(typeof(ProjectileOverlapAttack))]
public class ProjectileFinalOverlapHit : MonoBehaviour
{
    public DamageTypeCombo finalHitDamageType;
    public GameObject finalHitEffect;
    public float lifetime = 1f;

    void FixedUpdate()
    {
        lifetime -= Time.fixedDeltaTime;
        if (lifetime <= 0f)
        {
            var projectileDamage = GetComponent<ProjectileDamage>();
            projectileDamage.damageType |= finalHitDamageType;
            var projectileOverlapAttack = GetComponent<ProjectileOverlapAttack>();
            if (finalHitEffect)
            {
                projectileOverlapAttack.attack.hitEffectPrefab = finalHitEffect;
            }
            //projectileOverlapAttack.attack.impactSound = Addressables.LoadAssetAsync<NetworkSoundEventDef>(RoR2_Base_Merc.nseMercAssaulterImpact_asset).WaitForCompletion().index;
            projectileOverlapAttack.ResetOverlapAttack();
            projectileOverlapAttack.fireTimer = 0f;
            projectileOverlapAttack.MyFixedUpdate(0f);
            if (NetworkServer.active)
            {
                Destroy(gameObject);
            }
        }
    }
}
