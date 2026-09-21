using HG.GeneralSerializer;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ThrivingSurvivors.Core;

public static class Helpers
{
    public static void ModifyGameAssetAsync<TObject>(string key, Action<TObject> whenLoaded) where TObject : Object
    {
        Addressables.LoadAssetAsync<TObject>(key).Completed += loadOp =>
        {
            whenLoaded(loadOp.Result);
        };
    }

    extension(EntityStateConfiguration esc)
    {
        public bool SetFieldValue<T>(string fieldName, T value)
        {
            return esc.SetFieldValue(fieldName, typeof(T), value);
        }

        public bool SetFieldValue(string fieldName, Type fieldType, object value)
        {
            SerializedFieldCollection serializedFieldsCollection = esc.serializedFieldsCollection;
            if (serializedFieldsCollection.serializedFields == null)
            {
                return false;
            }
            for (int i = 0; i < serializedFieldsCollection.serializedFields.Length; i++)
            {
                ref SerializedField serializedField = ref serializedFieldsCollection.serializedFields[i];
                if (serializedField.fieldName == fieldName)
                {
                    if (typeof(Object).IsAssignableFrom(fieldType))
                    {
                        serializedField.fieldValue.objectValue = value as Object;
                        return true;
                    }
                    else if (StringSerializer.CanSerializeType(fieldType))
                    {
                        serializedField.fieldValue.stringValue = StringSerializer.Serialize(fieldType, value);
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }
    }

    extension (GameObject gameObject)
    {
        public bool RemoveComponentImmediate<TComponent>() where TComponent : Component
        {
            if (gameObject.TryGetComponent(out TComponent component))
            {
                Object.DestroyImmediate(component);
                return true;
            }
            return false;
        }
    }

    extension(Instruction instruction)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MatchGetProperty<T>(string name)
        {
            return instruction.MatchCallOrCallvirt<T>("get_" + name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MatchSetProperty<T>(string name)
        {
            return instruction.MatchCallOrCallvirt<T>("set_" + name);
        }
    }
}
