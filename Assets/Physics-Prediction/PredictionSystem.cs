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

namespace MB.PhysicsPrediction
{
    public static class PredictionSystem
    {
        public const string Path = "Prediction System/";

        public static class Objects
        {
            public static HashSet<PredictionObject> All { get; private set; }
            public static HashSet<PredictionRecorder> Recordable { get; private set; }

            public static PredictionObject Add(PredictionObject original)
            {
                var duplicate = Clone.Retrieve(original);

                original.Other = duplicate;
                duplicate.Other = original;

                All.Add(original);

                if (original is PredictionRecorder recordable)
                    Recordable.Add(recordable);

                duplicate.Freeze();

                return duplicate;
            }
            public static bool Remove(PredictionObject original)
            {
                if (All.Remove(original) == false)
                    return false;

                if (original is PredictionRecorder recordable)
                    Recordable.Remove(recordable);

                if (original && original.Other)
                    Object.Destroy(original.Other.gameObject);

                return true;
            }

            public static void Anchor()
            {
                foreach (var original in All)
                {
                    var duplicate = original.Other;
                    duplicate.Anchor();
                }
            }

            static Objects()
            {
                All = new();
                Recordable = new();
            }

            public static class Clone
            {
                public static Transform Container { get; private set; }

                internal static PredictionObject Retrieve(PredictionObject original)
                {
                    var scene = Scenes.Get(original.Mode);
                    scene.Validate();

                    var duplicate = Object.Instantiate(original, Container);
                    duplicate.name = original.name;

                    InitializeComponents(duplicate);
                    duplicate.transform.SetParent(default);
                    SceneManager.MoveGameObjectToScene(duplicate.gameObject, scene.Unity);

                    return duplicate;
                }

                /// <summary>
                /// Assign a method to evaluate component destruction on a per component basis
                /// </summary>
                public static EvaluateComponentPersistenceDelegate EvaluateComponentPersistence { get; set; }
                public delegate bool EvaluateComponentPersistenceDelegate(PredictionObject target, Component component);

                static List<Component> ComponentListCache = new();

                static void InitializeComponents(PredictionObject target)
                {
                    target.IsClone = true;

                    target.GetComponentsInChildren(true, ComponentListCache);

                    foreach (var component in ComponentListCache)
                    {
                        if (component is Transform) continue;

                        if (component is Collider) continue;
                        if (component is Collider2D) continue;

                        if (component is Rigidbody) continue;
                        if (component is Rigidbody2D) continue;

                        if (component is PredictionObject) continue;

                        if (component is IPredictionPersistantObject persistant)
                        {
                            persistant.IsClone = true;
                            continue;
                        }

                        if (EvaluateComponentPersistence?.Invoke(target, component) == true)
                            continue;

                        Object.DestroyImmediate(component);
                    }
                }

                static Clone()
                {
                    var gameObject = new GameObject("Prediction Clone Container");

                    gameObject.SetActive(false);
                    Object.DontDestroyOnLoad(gameObject);

                    Container = gameObject.transform;
                }
            }
        }

        public static class Prefabs
        {
            public static Dictionary<PredictionRecorder, Entry> Collection { get; private set; }
            public readonly struct Entry
            {
                public readonly GameObject Prefab { get; }

                public readonly PredictionRecorder Original { get; }
                public readonly PredictionRecorder Clone => Original.Other;

                public readonly Action<GameObject> Action { get; }

                internal void Start()
                {
                    Original.gameObject.SetActive(true);

                    Action(Clone.gameObject);
                }
                internal void End()
                {
                    Original.gameObject.SetActive(false);
                }

                public Entry(GameObject prefab, PredictionRecorder original, Action<GameObject> action)
                {
                    this.Prefab = prefab;
                    this.Original = original;
                    this.Action = action;
                }
            }

            public static PredictionRecorder Add(GameObject prefab, Action<GameObject> action)
            {
                var original = Object.Instantiate(prefab).GetComponent<PredictionRecorder>();

                if (original == null)
                    throw new ArgumentException($"Prefab Has no Prediction Object Component");

                original.Rename($"{prefab.name} - Predicated");

                var entry = new Entry(prefab, original, action);
                Collection.Add(original, entry);

                return original;
            }
            public static bool Remove(PredictionRecorder target)
            {
                if (Collection.Remove(target, out var entry) == false)
                    return false;

                var duplicate = target.Other;

                if(duplicate && duplicate.gameObject) Object.Destroy(duplicate.gameObject);

                return true;
            }

            public static void Start()
            {
                foreach (var entry in Collection.Values)
                    entry.Start();
            }
            public static void End()
            {
                foreach (var entry in Collection.Values)
                    entry.End();
            }

            static Prefabs()
            {
                Collection = new Dictionary<PredictionRecorder, Entry>();
            }
        }

        public static class Scenes
        {
            public const string ID = "Prediction";

            public static Physics2DController Physics2D { get; private set; }
            public class Physics2DController : Controller<PhysicsScene2D>
            {
                public override LocalPhysicsMode LocalPhysicsMode => LocalPhysicsMode.Physics2D;

                protected override PhysicsScene2D GetPhysicsScene(Scene scene) => scene.GetPhysicsScene2D();

                public override void Simulate(float step) => Physics.Simulate(step);

                public Physics2DController(string ID) : base(ID) { }
            }

            public static Physics3DController Physics3D { get; private set; }
            public class Physics3DController : Controller<PhysicsScene>
            {
                public override LocalPhysicsMode LocalPhysicsMode => LocalPhysicsMode.Physics3D;

                protected override PhysicsScene GetPhysicsScene(Scene scene) => scene.GetPhysicsScene();

                public override void Simulate(float step) => Physics.Simulate(step);

                public Physics3DController(string ID) : base(ID) { }
            }

            public abstract class Controller
            {
                public string ID { get; protected set; }

                public abstract LocalPhysicsMode LocalPhysicsMode { get; }

                public Scene Unity { get; private set; }

                public bool IsLoaded { get; private set; }

                internal void Prepare()
                {
                    SceneManager.sceneUnloaded += UnloadCallback;
                }

                public void Validate()
                {
                    if (IsLoaded) return;

                    Load();
                }
                internal virtual void Load()
                {
                    if (IsLoaded) throw new Exception($"{ID} Already Loaded");

                    var parameters = new CreateSceneParameters()
                    {
                        localPhysicsMode = LocalPhysicsMode,
                    };

                    Unity = SceneManager.CreateScene(ID, parameters);

                    IsLoaded = true;
                }

                void UnloadCallback(Scene scene)
                {
                    if (scene != Unity) return;

                    IsLoaded = false;
                }

                public abstract void Simulate(float time);

                public Controller(string ID)
                {
                    this.ID = ID;
                }
            }
            public abstract class Controller<T> : Controller
            {
                public T Physics { get; private set; }

                internal override void Load()
                {
                    base.Load();

                    Physics = GetPhysicsScene(Unity);
                }

                protected abstract T GetPhysicsScene(Scene scene);

                public Controller(string ID) : base(ID) { }
            }

            public static Controller Get(PredictionPhysicsMode mode)
            {
                switch (mode)
                {
                    case PredictionPhysicsMode.Physics2D:
                        return Physics2D;

                    case PredictionPhysicsMode.Physics3D:
                        return Physics3D;
                }

                throw new NotImplementedException();
            }

            internal static void Prepare()
            {
                Physics2D.Prepare();
                Physics3D.Prepare();
            }

            internal static void Simulate(float time)
            {
                if (Physics2D.IsLoaded) Physics2D.Simulate(time);
                if (Physics3D.IsLoaded) Physics3D.Simulate(time);
            }

            static Scenes()
            {
                Physics2D = new Physics2DController($"{ID} 2D");
                Physics3D = new Physics3DController($"{ID} 3D");
            }
        }

        public static class Record
        {
            internal static void Start()
            {
                foreach (var original in Objects.All)
                {
                    var clone = original.Other;

                    clone.Free();
                    clone.Anchor();
                }

                foreach (var original in Objects.Recordable)
                {
                    var clone = original.Other;
                    clone.Begin();
                }

                Prefabs.Start();
            }

            internal static void Capture()
            {
                foreach (var original in Objects.Recordable)
                {
                    var clone = original.Other;

                    clone.Capture();
                }
            }

            internal static void End()
            {
                foreach (var original in Objects.All)
                {
                    var clone = original.Other;

                    clone.Freeze();
                    clone.Anchor();
                }

                Prefabs.End();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnLoad()
        {
#if UNITY_EDITOR
            ///In Editor only as a visual showcase of the anchoring of objects
            ///In Reality we only need to anchor objects before we simulate
            RegisterPlayerLoop<PreLateUpdate>(Objects.Anchor);
#endif

            Scenes.Prepare();
        }

        public delegate void SimualateDelegate(int iterations);
        public static event SimualateDelegate OnSimulate;
        public static void Simulate(int iterations)
        {
            Record.Start();

            for (int i = 1; i <= iterations; i++)
            {
                Scenes.Simulate(Time.fixedDeltaTime);

                Record.Capture();
            }

            Record.End();

            OnSimulate?.Invoke(iterations);
        }

        //Utility

        internal static void RegisterPlayerLoop<TType>(PlayerLoopSystem.UpdateFunction callback)
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(TType))
                    loop.subSystemList[i].updateDelegate += callback;

            PlayerLoop.SetPlayerLoop(loop);
        }

        #region Physics Mode
        public static LocalPhysicsMode ConvertPhysicsMode(PredictionPhysicsMode mode)
        {
            switch (mode)
            {
                case PredictionPhysicsMode.Physics2D:
                    return LocalPhysicsMode.Physics2D;

                case PredictionPhysicsMode.Physics3D:
                    return LocalPhysicsMode.Physics3D;
            }

            throw new NotImplementedException();
        }

        public static PredictionPhysicsMode CheckPhysicsMode(GameObject gameObject) => CheckPhysicsMode(gameObject, PredictionPhysicsMode.Physics3D);
        public static PredictionPhysicsMode CheckPhysicsMode(GameObject gameObject, PredictionPhysicsMode fallback)
        {
            if (Has<Collider>()) return PredictionPhysicsMode.Physics3D;
            if (Has<Collider2D>()) return PredictionPhysicsMode.Physics2D;

            return fallback;

            bool Has<T>()
            {
                var component = gameObject.GetComponentInChildren<T>(true);

                return component != null;
            }
        }
        #endregion
    }

    public enum PredictionPhysicsMode
    {
        Physics2D,
        Physics3D,
    }

    public interface IPredictionPersistantObject
    {
        bool IsClone { get; set; }
    }
}