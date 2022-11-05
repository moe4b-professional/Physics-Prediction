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
using UnityEngine.Pool;

namespace MB.PhysicsPrediction
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExectionOrder)]
    [AddComponentMenu(PredictionSystem.Path + "Prediction Recorder")]
    public class PredictionRecorder : PredictionObject
    {
        public List<PredictionCoordinate> Coordinates { get; private set; }
        [Serializable]
        public struct PredictionCoordinate
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public PredictionCoordinate(Vector3 position, Quaternion rotation)
            {
                this.Position = position;
                this.Rotation = rotation;
            }
        }

        public List<CollisionData3D> Collisions3D { get; private set; }
        [Serializable]
        public struct CollisionData3D
        {
            public PredictionObject Target { get; private set; }

            public Collision Context { get; private set; }

            public int ContactCount => Context.contactCount;
            public ContactPoint GetContact(int index) => Context.GetContact(index);

            public Vector3 Impulse => Context.impulse;
            public Vector3 RelativeVelocity => Context.relativeVelocity;

            public CollisionData3D(PredictionObject target, Collision context)
            {
                this.Target = target;
                this.Context = context;
            }
        }

        public List<CollisionData2D> Collisions2D { get; private set; }
        [Serializable]
        public struct CollisionData2D
        {
            public PredictionObject Target { get; private set; }

            public Collision2D Context { get; private set; }

            public int ContactCount => Context.contactCount;
            public ContactPoint2D GetContact(int index) => Context.GetContact(index);

            public Vector2 RelativeVelocity => Context.relativeVelocity;

            public CollisionData2D(PredictionObject target, Collision2D context)
            {
                this.Target = target;
                this.Context = context;
            }
        }

        [field: NonSerialized]
        public new PredictionRecorder Other { get; internal set; }

        protected override void Awake()
        {
            base.Awake();

            Other = base.Other as PredictionRecorder;

            if (IsOriginal)
            {
                Coordinates = Other.Coordinates;

                Collisions3D = Other.Collisions3D;
                Collisions2D = Other.Collisions2D;
            }
            else
            {
                Coordinates = ListPool<PredictionCoordinate>.Get();

                Collisions3D = ListPool<CollisionData3D>.Get();
                Collisions2D = ListPool<CollisionData2D>.Get();
            }
        }

        bool InSimulation;
        public bool StopSimulation { get; private set; }

        internal void Begin()
        {
            InSimulation = true;
            StopSimulation = false;

            Coordinates.Clear();
            Collisions3D.Clear();
        }
        internal void Capture()
        {
            if (StopSimulation)
                return;

            var coordinate = new PredictionCoordinate(Position, Rotation);
            Coordinates.Add(coordinate);
        }
        internal void End()
        {
            InSimulation = false;
            StopSimulation = false;
        }

        protected override void OnCollisionEnter(Collision collision)
        {
            base.OnCollisionEnter(collision);

            if (IsClone)
            {
                if (InSimulation)
                {
                    if (TryGetCollisionRoot(collision, out var target) == false)
                    {
                        Debug.LogWarning($"Prediction Recorder Collision Couldn't Result in a Collider Prediction Object, Setup Might be Wrong");
                        return;
                    }

                    var data = new CollisionData3D(target.Other, collision);
                    Collisions3D.Add(data);

                    if (target.TryGetComponent<PredictionBlocker>(out var blocker))
                    {
                        StopSimulation = true;
                        Freeze();
                    }
                }
            }

            static bool TryGetCollisionRoot(Collision collision, out PredictionObject target)
            {
                target = collision.collider.GetComponentInParent<PredictionObject>(true);
                return target != null;
            }
        }
        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);

            if (IsClone)
            {
                if (InSimulation)
                {
                    if (TryGetCollisionRoot(collision, out var target) == false)
                    {
                        Debug.LogWarning($"Prediction Recorder Collision Couldn't Result in a Collider Prediction Object, Setup Might be Wrong");
                        return;
                    }

                    var data = new CollisionData2D(target.Other, collision);
                    Collisions2D.Add(data);

                    if (target.TryGetComponent<PredictionBlocker>(out var blocker))
                    {
                        StopSimulation = true;
                        Freeze();
                    }
                }
            }

            static bool TryGetCollisionRoot(Collision2D collision, out PredictionObject target)
            {
                target = collision.collider.GetComponentInParent<PredictionObject>(true);
                return target != null;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if(IsClone)
            {
                ListPool<PredictionCoordinate>.Release(Coordinates);

                ListPool<CollisionData3D>.Release(Collisions3D);
                ListPool<CollisionData2D>.Release(Collisions2D);
            }
        }
    }
}