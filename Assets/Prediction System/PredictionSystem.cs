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

        #region Process
        public static int Iterations { get; private set; }

        public delegate void StartDelegate();
        public static event StartDelegate OnStart;
        public static void Start(int iterations)
        {
            Iterations = iterations;

            OnStart?.Invoke();
        }

        public static Vector3[] RecordPrefab(GameObject prefab, Action<GameObject> action)
        {
            Scenes.Validate();

            var instance = Clone(prefab);
            action(instance);

            var points = new Vector3[Iterations];

            OnIterate += Register;
            void Register(int index) => points[index] = instance.transform.position;

            OnEnd += Clear;
            void Clear() => Object.Destroy(instance);

            return points;
        }

        public static Vector3[] RecordObject(PredictionObject target)
        {
            Scenes.Validate();

            var points = new Vector3[Iterations];

            OnIterate += Register;
            void Register(int index) => points[index] = target.Clone.Position;

            return points;
        }
        
        public static void Simulate()
        {
            for (int i = 0; i < Iterations; i++)
                Iterate(i);

            End();
        }

        public delegate void IterateDelegate(int frame);
        public static event IterateDelegate OnIterate;
        static void Iterate(int frame)
        {
            Scenes.Physics.Simulate(Time.fixedDeltaTime);

            OnIterate?.Invoke(frame);
        }

        public delegate void EndDelegate();
        public static event EndDelegate OnEnd;
        static void End()
        {
            Objects.Anchor();

            OnIterate = null;

            OnEnd?.Invoke();
        }
        #endregion

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
    }

    public interface IPredictionPersistantObject { }
}