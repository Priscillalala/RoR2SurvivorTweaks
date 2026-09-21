using RoR2;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ThrivingSurvivors.Core;

public readonly ref struct ModifyEntityStateAsync(string key)
{
    readonly AsyncOperationHandle<EntityStateConfiguration> loadOp = Addressables.LoadAssetAsync<EntityStateConfiguration>(key);

    public object this[string fieldName]
    {
        set
        {
            if (loadOp.IsDone)
            {
                SetFieldValue(loadOp);
            }
            else
            {
                loadOp.Completed += SetFieldValue;
            }
            
            void SetFieldValue(AsyncOperationHandle<EntityStateConfiguration> completedLoadOp)
            {
                completedLoadOp.Result.SetFieldValue(fieldName, value.GetType(), value);
            }
        }
    }
}
