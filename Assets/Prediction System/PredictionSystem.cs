using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Default
{
    public static class PredictionSystem
    {
        public static class Objects
        {
            public static Dictionary<PredictionObject, PredictionObject> Dictionary { get; private set; }

            internal static void Prepare()
            {

            }

            public static PredictionObject Add(PredictionObject target)
            {
                Scenes.Validate();

                var copy = Clone(target.gameObject).GetComponent<PredictionObject>();

                target.Other = copy;
                copy.Other = target;

                Dictionary.Add(target, copy);

                return copy;
            }

            public static bool Remove(PredictionObject target)
            {
                if (Dictionary.Remove(target) == false)
                    return false;

                if (target && target.Other && target.Other.gameObject)
                    Object.Destroy(target.Other.gameObject);

                return true;
            }

            public static void Clear()
            {
                Dictionary.Clear();
            }

            public static void Anchor()
            {
                foreach (var pair in Dictionary)
                {
                    var original = pair.Key;
                    var copy = pair.Value;

                    copy.transform.position = original.transform.position;
                    copy.transform.rotation = original.transform.rotation;
                    copy.transform.localScale = original.transform.localScale;

                    if (original.HasRigidbody)
                    {
                        copy.rigidbody.position = original.rigidbody.position;
                        copy.rigidbody.rotation = original.rigidbody.rotation;

                        copy.rigidbody.velocity = Vector3.zero;
                        copy.rigidbody.angularVelocity = Vector3.zero;
                    }
                }
            }

            static Objects()
            {
                Dictionary = new Dictionary<PredictionObject, PredictionObject>();
            }
        }

        public static class Scenes
        {
            public const string Name = "Prediction Scene";

            public static Scene Unity { get; private set; }
            public static PhysicsScene Physics { get; private set; }

            public static bool IsLoaded { get; private set; }

            internal static void Prepare()
            {
                SceneManager.sceneUnloaded += UnloadCallback;
            }

            static void UnloadCallback(Scene scene)
            {
                if (scene != Unity) return;

                IsLoaded = false;
                Objects.Clear();
                Record.Clear();
            }

            public static void Validate()
            {
                if (IsLoaded) return;

                Load();
            }
            internal static void Load()
            {
                if (IsLoaded) throw new Exception($"{Name} Already Loaded");

                var paramters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
                Unity = SceneManager.LoadScene(Name, paramters);
                IsLoaded = true;

                Physics = Unity.GetPhysicsScene();
            }
        }

        public static class Layers
        {
            public const string Name = "Prediction";

            public static int Index { get; private set; } = LayerMask.NameToLayer(Name);

            internal static void Prepare()
            {
                for (int layer = 0; layer < 31; layer++)
                {
                    var ignore = layer != Index;
                    Physics.IgnoreLayerCollision(Index, layer, ignore);
                }
            }

            public static void Set(GameObject gameObject) => Set(gameObject, Index);
            public static void Set(GameObject gameObject, int layer)
            {
                gameObject.layer = layer;

                foreach (Transform child in gameObject.transform)
                    Set(child.gameObject, layer);
            }
        }

        public static class Record
        {
            public static class Objects
            {
                public static Dictionary<PredictionObject, Timeline> Dictionary { get; private set; }

                public static Timeline Add(PredictionObject target)
                {
                    if (Dictionary.TryGetValue(target, out var points) == false)
                    {
                        points = new Timeline();
                        Dictionary[target] = points;
                    }

                    return points;
                }

                internal static void Prepare()
                {
                    foreach (var timeline in Dictionary.Values)
                        timeline.Clear();
                }

                internal static void Iterate()
                {
                    foreach (var pair in Dictionary)
                    {
                        var target = pair.Key;
                        var timeline = pair.Value;

                        timeline.Add(target.Clone.Position);
                    }
                }

                internal static void Finish()
                {

                }

                public static void Remove(PredictionObject target)
                {
                    Dictionary.Remove(target);
                }

                internal static void Clear()
                {
                    Dictionary.Clear();
                }

                static Objects()
                {
                    Dictionary = new Dictionary<PredictionObject, Timeline>();
                }
            }

            public static class Prefabs
            {
                public static Dictionary<Timeline, Entry> Dictionary { get; private set; }

                public struct Entry
                {
                    public GameObject Prefab { get; private set; }
                    public GameObject Instance { get; private set; }

                    public Vector3 Position => Instance.transform.position;
                    public Quaternion Rotation => Instance.transform.rotation;

                    public Action<GameObject> Action { get; private set; }

                    internal void Prepare()
                    {
                        Instance.SetActive(true);

                        Action(Instance);
                    }

                    internal void Finish()
                    {
                        Instance.SetActive(false);
                    }

                    public Entry(GameObject prefab, GameObject instance, Action<GameObject> action)
                    {
                        this.Prefab = prefab;
                        this.Instance = instance;
                        this.Action = action;
                    }
                }

                public static Timeline Add(GameObject prefab, Action<GameObject> action)
                {
                    var timeline = new Timeline();

                    var instance = Clone(prefab);
                    instance.SetActive(false);

                    var entry = new Entry(prefab, instance, action);

                    Dictionary.Add(timeline, entry);

                    return timeline;
                }

                internal static void Prepare()
                {
                    foreach (var pair in Dictionary)
                    {
                        var timeline = pair.Key;
                        timeline.Clear();

                        var entry = pair.Value;
                        entry.Prepare();
                    }
                }

                internal static void Iterate()
                {
                    foreach (var pair in Dictionary)
                    {
                        var timeline = pair.Key;
                        var entry = pair.Value;

                        timeline.Add(entry.Position);
                    }
                }

                internal static void Finish()
                {
                    foreach (var entry in Dictionary.Values)
                        entry.Finish();
                }

                public static bool Remove(Timeline timeline)
                {
                    if(Dictionary.TryGetValue(timeline, out var entry))
                        Object.Destroy(entry.Instance);

                    return Dictionary.Remove(timeline);
                }

                internal static void Clear()
                {
                    foreach (var entry in Dictionary.Values)
                    {
                        var instance = entry.Instance;

                        Object.Destroy(instance);
                    }

                    Dictionary.Clear();
                }

                static Prefabs()
                {
                    Dictionary = new Dictionary<Timeline, Entry>();
                }
            }

            internal static void Prepare()
            {
                Objects.Prepare();
                Prefabs.Prepare();
            }

            internal static void Iterate()
            {
                Objects.Iterate();
                Prefabs.Iterate();
            }

            internal static void Finish()
            {
                Objects.Finish();
                Prefabs.Finish();
            }

            internal static void Clear()
            {
                Objects.Clear();
                Prefabs.Clear();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare()
        {
            RegisterPlayerLoop<Update>(Update);

            Objects.Prepare();
            Scenes.Prepare();
            Layers.Prepare();
        }

        static void Update()
        {
            Objects.Anchor();
        }

        public delegate void SimualateDelegate(int iterations);
        public static event SimualateDelegate OnSimulate;
        public static void Simulate(int iterations)
        {
            Scenes.Validate();

            Record.Prepare();

            for (int i = 0; i < iterations; i++)
            {
                Scenes.Physics.Simulate(Time.fixedDeltaTime);

                Record.Iterate();
            }

            Record.Finish();
            Objects.Anchor();

            OnSimulate?.Invoke(iterations);
        }

        //Utility

        public static void RegisterPlayerLoop<TType>(PlayerLoopSystem.UpdateFunction callback)
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(TType))
                    loop.subSystemList[i].updateDelegate += callback;

            PlayerLoop.SetPlayerLoop(loop);
        }

        public static GameObject Clone(GameObject source)
        {
            Scenes.Validate();

            PredictionObject.CloneFlag = true;
            var instance = Object.Instantiate(source);
            instance.name = source.name;

            SceneManager.MoveGameObjectToScene(instance, Scenes.Unity);
            PredictionObject.CloneFlag = false;

            Layers.Set(instance);
            DestoryAllNonEssentialComponents(instance);

            return instance;
        }

        public static void DestoryAllNonEssentialComponents(GameObject gameObject)
        {
            var components = gameObject.GetComponentsInChildren<Component>(true);

            foreach (var component in components)
            {
                if (component is Transform) continue;
                if (component is Rigidbody) continue;
                if (component is Collider) continue;
                if (component is Collider2D) continue;
                if (component is IPredictionPersistantObject) continue;

                Object.Destroy(component);
            }
        }

        public class Timeline : List<Vector3>
        {

        }
    }

    public interface IPredictionPersistantObject { }
}