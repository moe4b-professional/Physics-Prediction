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
        public static Dictionary<PredictionObject, PredictionObject> Objects { get; private set; }

        public const string SceneName = "Prediction Scene";

        public static Scene UnityScene { get; private set; }
        public static PhysicsScene PhysicsScene { get; private set; }

        public static bool IsSceneLoaded { get; private set; }

        public const string LayerName = "Prediction";
        public static int LayerIndex { get; private set; } = LayerMask.NameToLayer(LayerName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare()
        {
            RegisterPlayerLoop<Update>(Update);

            SceneManager.sceneUnloaded += SceneUnloadedCallback;

            for (int i = 0; i < 31; i++)
            {
                var ignore = i != LayerIndex;
                Physics.IgnoreLayerCollision(LayerIndex, i, ignore);
            }
        }

        static void SceneUnloadedCallback(Scene scene)
        {
            if (scene != UnityScene) return;

            IsSceneLoaded = false;
            Objects.Clear();
        }

        static void ValidateScene()
        {
            if (IsSceneLoaded) return;

            LoadScene();
        }
        static void LoadScene()
        {
            if (IsSceneLoaded) throw new Exception($"{SceneName} Already Loaded");

            var paramters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
            UnityScene = SceneManager.LoadScene(SceneName, paramters);
            IsSceneLoaded = true;

            PhysicsScene = UnityScene.GetPhysicsScene();
        }

        static void Update()
        {
            AnchorObjects();
        }

        #region Objects
        public static PredictionObject AddObject(PredictionObject target)
        {
            ValidateScene();

            var copy = Clone(target.gameObject).GetComponent<PredictionObject>();

            target.Other = copy;
            copy.Other = target;

            Objects.Add(target, copy);

            return copy;
        }

        public static bool RemoveObject(PredictionObject target)
        {
            if (Objects.Remove(target) == false)
                return false;

            if (target && target.Other && target.Other.gameObject)
                Object.Destroy(target.Other.gameObject);

            return true;
        }

        public static void AnchorObjects()
        {
            foreach (var pair in Objects)
            {
                var original = pair.Key;
                var copy = pair.Value;

                copy.transform.position = original.transform.position;
                copy.transform.rotation = original.transform.rotation;
                copy.transform.localScale = original.transform.localScale;

                if(original.HasRigidbody)
                {
                    copy.rigidbody.position = original.rigidbody.position;
                    copy.rigidbody.rotation = original.rigidbody.rotation;

                    copy.rigidbody.velocity = Vector3.zero;
                    copy.rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }
        #endregion

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
            ValidateScene();

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
            ValidateScene();

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
            PhysicsScene.Simulate(Time.fixedDeltaTime);

            OnIterate?.Invoke(frame);
        }

        public delegate void EndDelegate();
        public static event EndDelegate OnEnd;
        static void End()
        {
            AnchorObjects();

            OnIterate = null;

            OnEnd?.Invoke();
        }
        #endregion

        static PredictionSystem()
        {
            Objects = new Dictionary<PredictionObject, PredictionObject>();
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

        public static bool CloneFlag { get; private set; }
        public static GameObject Clone(GameObject source)
        {
            CloneFlag = true;
            var instance = Object.Instantiate(source);

            instance.name = source.name;

            SceneManager.MoveGameObjectToScene(instance, UnityScene);
            CloneFlag = false;

            SetLayer(instance, LayerIndex);
            DestoryAllNonEssentialComponents(instance);

            return instance;
        }

        public static void SetLayer(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;

            foreach (Transform child in gameObject.transform)
                SetLayer(child.gameObject, layer);
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