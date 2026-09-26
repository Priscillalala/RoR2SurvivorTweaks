using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

public static class Prefab
{
    private static Transform prefabParent;

    public static void Init()
    {
        GameObject prefabParentObject = new GameObject(Plugin.GUID + "_Prefabs");
        prefabParentObject.SetActive(false);
        Object.DontDestroyOnLoad(prefabParentObject);
        prefabParent = prefabParentObject.transform;
        Plugin.Harmony.PatchAll(typeof(Prefab));
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Util), nameof(Util.IsPrefab))]
    static bool IsPrefab(bool isPrefab, GameObject gameObject)
    {
        return isPrefab || gameObject.transform.parent == prefabParent;
    }

    public static GameObject Clone(GameObject original, string name)
    {
        GameObject prefab = Object.Instantiate(original, prefabParent);
        prefab.name = name;
        if (prefab.TryGetComponent(out NetworkIdentity networkIdentity))
        {
            networkIdentity.assetId.Reset();
        }
        return prefab;
    }

    public static GameObject Create(string name)
    {
        GameObject prefab = new GameObject(name);
        prefab.transform.SetParent(prefabParent);
        return prefab;
    }

    public static GameObject Create(string name, params Type[] components)
    {
        GameObject prefab = new GameObject(name, components);
        prefab.transform.SetParent(prefabParent);
        return prefab;
    }
}