using HG.GeneralSerializer;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ThrivingSurvivors.Core.Components;

[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileOverlapAttack))]
public class ProjectileOverlapUseAttackSpeed : MonoBehaviour
{
    void Start()
    {
        var projectileController = GetComponent<ProjectileController>();
        if (projectileController.owner && projectileController.owner.TryGetComponent(out CharacterBody ownerBody))
        {
            float ownerAttackSpeedStat = ownerBody.attackSpeed;
            var projectileOverlapAttack = GetComponent<ProjectileOverlapAttack>();
            projectileOverlapAttack.fireFrequency *= ownerAttackSpeedStat;
            if (projectileOverlapAttack.resetInterval > 0f)
            {
                projectileOverlapAttack.resetInterval /= ownerAttackSpeedStat;
            }
        }
    }
}
