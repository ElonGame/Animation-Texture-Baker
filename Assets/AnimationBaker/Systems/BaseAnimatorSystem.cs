using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using AnimationBaker.Interfaces;

namespace AnimationBaker.Systems
{
    public abstract class BaseAnimatorSystem<T> : JobComponentSystem where T : struct, IComponentData
    {
        private List<FieldInfo> fieldInfos = new List<FieldInfo>();
        private List<string> fieldNames = new List<string>();
        private Queue<QueuedStateData> queuedUpdates = new Queue<QueuedStateData>();
        [Inject] private ComponentDataFromEntity<T> currentData;

        protected override void OnCreateManager(int capacity)
        {
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                fieldInfos.Add(field);
                fieldNames.Add(field.Name);
            }
        }

        protected virtual void ApplyUpdates(ComponentDataFromEntity<T> currentData)
        {
            while (queuedUpdates.Count > 0)
            {
                var data = queuedUpdates.Dequeue();
                object boxed = currentData[data.entity];
                if (data.isFloat)
                {
                    data.field.SetValue(boxed, data.FloatVal);
                }
                else
                {
                    data.field.SetValue(boxed, data.IntVal);
                }
                currentData[data.entity] = (T) boxed;
            }
        }

        public virtual void SetBool(Entity entity, string key, bool value)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            var setValue = value ? 1 : 0;
            queuedUpdates.Enqueue(new QueuedStateData { entity = entity, field = field, IntVal = setValue });
        }

        public virtual void SetFloat(Entity entity, string key, float value)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            queuedUpdates.Enqueue(new QueuedStateData { entity = entity, field = field, FloatVal = value, isFloat = true });
        }

        public virtual void SetInt(Entity entity, string key, int value)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            queuedUpdates.Enqueue(new QueuedStateData { entity = entity, field = field, IntVal = value });
        }

        public virtual void SetTrigger(Entity entity, string key)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            queuedUpdates.Enqueue(new QueuedStateData { entity = entity, field = field, IntVal = 1 });
        }

        public virtual void ResetTrigger(Entity entity, string key)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            queuedUpdates.Enqueue(new QueuedStateData { entity = entity, field = field, IntVal = 0 });
        }

        public virtual bool GetBool(Entity entity, string key)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return false;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            var data = currentData[entity];
            var value = (int) field.GetValue(data);
            if (value == 1)
            {
                return true;
            }
            return false;
        }

        public virtual float GetFloat(Entity entity, string key)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return 0;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            var data = currentData[entity];
            return (float) field.GetValue(data);
        }

        public virtual int GetInt(Entity entity, string key)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return 0;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            var data = currentData[entity];
            return (int) field.GetValue(data);
        }

        public virtual bool GetTrigger(Entity entity, string key)
        {
            if (!fieldNames.Contains(key))
            {
                Debug.LogWarning($"Key {key} does not exist");
                return false;
            }
            var field = fieldInfos[fieldNames.IndexOf(key)];
            var data = currentData[entity];
            var value = (int) field.GetValue(data);
            if (value == 1)
            {
                return true;
            }
            return false;
        }

        private struct QueuedStateData
        {
            public Entity entity;
            public FieldInfo field;
            public bool isFloat;
            public float FloatVal;
            public int IntVal;
        }
    }
}
