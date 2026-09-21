using UnityEngine;

namespace ThrivingSurvivors.Core;

public readonly ref struct ModifyComponent<TComponent>(GameObject gameObject) where TComponent : Component
{
    public readonly TComponent c = gameObject.GetComponent<TComponent>();

    public static implicit operator TComponent(ModifyComponent<TComponent> getComponent)
    {
        return getComponent.c;
    }
}
