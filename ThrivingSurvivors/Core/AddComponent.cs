using UnityEngine;

namespace ThrivingSurvivors.Core;

public readonly ref struct AddComponent<TComponent>(GameObject gameObject) where TComponent : Component
{
    public readonly TComponent c = gameObject.AddComponent<TComponent>();

    public static implicit operator TComponent(AddComponent<TComponent> addComponent)
    {
        return addComponent.c;
    }
}
