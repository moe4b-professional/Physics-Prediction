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
            public static Dictionary<PredictionObject, PredictionObject> Collection { get; private set; }

            public static PredictionObject Add(PredictionObject target, PredictionPhysicsMode mode)
            {
                var copy = Clone.Retrieve(target.gameObject, mode).GetComponent<PredictionObject>();

                target.Other = copy;
                copy.Other = target;

                Collection.Add(target, copy);

                return copy;
            }

            public static bool Remove(PredictionObject target)
            {
                if (Collection.Remove(target) == false)
                    return false;

                if (target && target.Other && target.Other.gameObject)
                    Object.Destroy(target.Other.gameObject);

                return true;
            }

            internal static void Clear()
            {
                Collection.Clear();
            }

            #region Anchor
            public static void Anchor()
            {
                foreach (var pair in Collection)
                {
                    var original = pair.Key;
                    var copy = pair.Value;

                    AnchorTransform(original.transform, copy.transform);

                    if (original.HasRigidbody) AnchorRigidbody(original.rigidbody, copy.rigidbody);
                    if (original.HasRigidbody2D) AnchorRigidbody2D(original.rigidbody2D, copy.rigidbody2D);
                }
            }

            internal static void AnchorTransform(Transform original, Transform copy)
            {
                copy.position = original.position;
                copy.rotation = original.rotation;
                copy.localScale = original.localScale;
            }

            internal static void AnchorRigidbody(Rigidbody original, Rigidbody copy)
            {
                copy.position = original.position;
                copy.rotation = original.rotation;

                copy.velocity = original.velocity;
                copy.angularVelocity = original.angularVelocity;
            }

            internal static void AnchorRigidbody2D(Rigidbody2D original, Rigidbody2D copy)
            {
                copy.position = original.position;
                copy.rotation = original.rotation;

                copy.velocity = original.velocity;
                copy.angularVelocity = original.angularVelocity;
            }
            #endregion

            static Objects()
            {
                Collection = new Dictionary<PredictionObject, PredictionObject>();
            }
        }

        public static class Clone
        {
            public static bool Flag { get; private set; }
            internal static GameObject Retrieve(GameObject source, PredictionPhysicsMode mode)
            {
                Flag = true;

                Scenes.Get(mode).Validate();

                var instance = Object.Instantiate(source);
                instance.name = source.name;

                var scene = Scenes.Get(mode);

                SceneManager.MoveGameObjectToScene(instance, scene.Unity);

                DestoryAllNonPersistentComponents(instance);

                Flag = false;

                return instance;
            }

            /// <summary>
            /// Add a type to it to ensure it doesn't get destroyed when creating a clone
            /// </summary>
            public static HashSet<Type> PersistentComponents { get; private set; } = new HashSet<Type>
            {
                typeof(Transform),

                typeof(Rigidbody),
                typeof(Rigidbody2D),
            };

            /// <summary>
            /// Assign a method to evaluate component destruction on a per component basis
            /// </summary>
            public static EvaluateComponentPersistenceDelegate EvaluateComponentPersistence { get; set; }
            public delegate bool EvaluateComponentPersistenceDelegate(GameObject gameObject, Component component);

            static void DestoryAllNonPersistentComponents(GameObject gameObject)
            {
                var components = gameObject.GetComponentsInChildren<Component>(true);

                foreach (var component in components)
                {
                    if (component is Collider) continue;
                    if (component is Collider2D) continue;
                    if (component is IPredictionPersistantObject) continue;

                    var type = component.GetType();
                    if (PersistentComponents.Contains(type)) continue;

                    if (EvaluateComponentPersistence != null) if (EvaluateComponentPersistence(gameObject, component)) continue;

                    Object.DestroyImmediate(component);
                }
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
                    Objects.Clear();
                    Record.Clear();
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
            public static class Objects
            {
                public static Dictionary<PredictionTimeline, PredictionObject> Collection { get; private set; }

                public static PredictionTimeline Add(PredictionObject target)
                {
                    if (target.IsClone)
                        throw new ArgumentException("Cannot Record A Clone, Please Pass in the Original Object");

                    var timeline = new PredictionTimeline();
                    Collection[timeline] = target;

                    return timeline;
                }

                #region Procedure
                internal static void Prepare()
                {
                    foreach (var timeline in Collection.Keys)
                        timeline.Clear();
                }

                internal static void Iterate()
                {
                    foreach (var pair in Collection)
                    {
                        var timeline = pair.Key;
                        var target = pair.Value;

                        timeline.Add(target.Clone.Position, target.Clone.Rotation);
                    }
                }

                internal static void Finish()
                {

                }
                #endregion

                public static bool Remove(PredictionTimeline timeline)
                {
                    return Collection.Remove(timeline);
                }

                internal static void Clear()
                {
                    Collection.Clear();
                }

                static Objects()
                {
                    Collection = new Dictionary<PredictionTimeline, PredictionObject>();
                }
            }

            public static class Prefabs
            {
                public static Dictionary<PredictionTimeline, Entry> Collection { get; private set; }

                public class Entry
                {
                    public GameObject Prefab { get; private set; }
                    public GameObject Instance { get; private set; }

                    public Rigidbody Rigidbody { get; private set; }
                    public Rigidbody2D Rigidbody2D { get; private set; }

                    public Vector3 Position => Instance.transform.position;
                    public Quaternion Rotation => Instance.transform.rotation;

                    public Action<GameObject> Action { get; private set; }

                    internal void Prepare()
                    {
                        Instance.transform.position = Prefab.transform.position;
                        Instance.transform.rotation = Prefab.transform.rotation;

                        if (Rigidbody)
                        {
                            Rigidbody.velocity = Vector3.zero;
                            Rigidbody.angularVelocity = Vector3.zero;
                        }

                        if (Rigidbody2D)
                        {
                            Rigidbody2D.velocity = Vector2.zero;
                            Rigidbody2D.angularVelocity = 0f;
                        }

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

                        Rigidbody = Instance.GetComponent<Rigidbody>();
                        Rigidbody2D = Instance.GetComponent<Rigidbody2D>();

                        this.Action = action;
                    }
                }

                public static PredictionTimeline Add(GameObject prefab, Action<GameObject> action)
                {
                    var mode = CheckPhysicsMode(prefab);

                    return Add(prefab, mode, action);
                }
                public static PredictionTimeline Add(GameObject prefab, PredictionPhysicsMode mode, Action<GameObject> action)
                {
                    var timeline = new PredictionTimeline();

                    var instance = Clone.Retrieve(prefab, mode);
                    instance.SetActive(false);

                    var entry = new Entry(prefab, instance, action);

                    Collection.Add(timeline, entry);

                    return timeline;
                }

                #region Procedure
                internal static void Prepare()
                {
                    foreach (var pair in Collection)
                    {
                        var timeline = pair.Key;
                        timeline.Clear();

                        var entry = pair.Value;
                        entry.Prepare();
                    }
                }

                internal static void Iterate()
                {
                    foreach (var pair in Collection)
                    {
                        var timeline = pair.Key;
                        var entry = pair.Value;

                        timeline.Add(entry.Position, entry.Rotation);
                    }
                }

                internal static void Finish()
                {
                    foreach (var entry in Collection.Values)
                        entry.Finish();
                }
                #endregion

                public static bool Remove(PredictionTimeline timeline)
                {
                    if (Collection.TryGetValue(timeline, out var entry))
                        Object.Destroy(entry.Instance);

                    return Collection.Remove(timeline);
                }

                internal static void Clear()
                {
                    foreach (var entry in Collection.Values)
                    {
                        var instance = entry.Instance;

                        Object.Destroy(instance);
                    }

                    Collection.Clear();
                }

                static Prefabs()
                {
                    Collection = new Dictionary<PredictionTimeline, Entry>();
                }
            }

            #region Procedure
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
            #endregion

            internal static void Clear()
            {
                Objects.Clear();
                Prefabs.Clear();
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
            Objects.Anchor();
            Record.Prepare();

            for (int i = 0; i < iterations; i++)
            {
                Scenes.Simulate(Time.fixedDeltaTime);

                Record.Iterate();
            }

            Record.Finish();

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

    public interface IPredictionPersistantObject { }
}