using JetBrains.Annotations;
using RoR2;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AssetLoader
{
    public readonly struct AddressableAssetHandle<TObject>(AsyncOperationHandle<TObject> internalHandle) where TObject : Object
    {
        private readonly AsyncOperationHandle<TObject> internalHandle = internalHandle;

        public TObject Asset => internalHandle.IsDone ? internalHandle.Result : throw new InvalidOperationException("addressable asset not loaded");

        public static implicit operator TObject(AddressableAssetHandle<TObject> handle) => handle.Asset;
    }

    public readonly ref struct RequestAddressableAssetsChain
    {
        public RequestAddressableAssetsChain And<TObject>(string key, out AddressableAssetHandle<TObject> handle) where TObject : Object
        {
            var internalHandle = Addressables.LoadAssetAsync<TObject>(key);
            requestedAddressableAssets.Add(internalHandle);
            handle = new(internalHandle);
            return this;
        }
    }

    public static event Action AssetsReady;

    static List<AsyncOperationHandle> requestedAddressableAssets = [];

    public static RequestAddressableAssetsChain RequestAddressableAssets<TObject>(string key, out AddressableAssetHandle<TObject> handle) where TObject : Object
    {
        if (requestedAddressableAssets == null)
        {
            throw new InvalidOperationException("too late for asset requests");
        }
        return new RequestAddressableAssetsChain().And(key, out handle);
    }

    [UsedImplicitly]
    [InitDuringStartupPhase(GameInitPhase.PreFrame)]
    static void WaitForAssets()
    {
        foreach (var internalHandle in requestedAddressableAssets)
        {
            if (!internalHandle.IsDone)
            {
                Plugin.Logger.LogWarning($"Waiting for addressable asset: {internalHandle.DebugName}");
                internalHandle.WaitForCompletion();
            }
        }
        AssetsReady?.Invoke();
        requestedAddressableAssets = null;
    }
}