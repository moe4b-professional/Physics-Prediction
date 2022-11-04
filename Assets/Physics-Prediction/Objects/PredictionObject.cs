using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;
using System;

namespace MB.PhysicsPrediction
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExectionOrder)]
    public abstract class PredictionObject : MonoBehaviour
    {
        public const int ExectionOrder = -200;

        [SerializeField]
        PredictionPhysicsMode mode = PredictionPhysicsMode.Physics3D;
        public PredictionPhysicsMode Mode => mode;

        public bool Active
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }
        public Quaternion Rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        public Rigidbody Rigidbody3D { get; protected set; }
        public Rigidbody2D Rigidbody2D { get; protected set; }

        public bool IsClone { get; internal set; }
        public bool IsOriginal => !IsClone;

        [field: NonSerialized]
        public PredictionObject Other { get; internal set; }

        protected virtual void OnValidate()
        {
            mode = PredictionSystem.CheckPhysicsMode(gameObject, mode);
        }

        protected virtual void Awake()
        {
            Rigidbody3D = GetComponent<Rigidbody>();
            Rigidbody2D = GetComponent<Rigidbody2D>();

            if (IsOriginal)
            {
                PredictionSystem.Objects.Add(this);
            }
        }

        public virtual void Anchor()
        {
            Position = Other.Position;
            Rotation = Other.Rotation;

            if (Rigidbody2D)
            {
                Rigidbody2D.velocity = Other.Rigidbody2D.velocity;
                Rigidbody2D.angularVelocity = Other.Rigidbody2D.angularVelocity;

                Rigidbody2D.position = Other.Rigidbody2D.position;
                Rigidbody2D.rotation = Other.Rigidbody2D.rotation;
            }

            if (Rigidbody3D)
            {
                Rigidbody3D.velocity = Other.Rigidbody3D.velocity;
                Rigidbody3D.angularVelocity = Other.Rigidbody3D.angularVelocity;

                Rigidbody3D.position = Other.Rigidbody3D.position;
                Rigidbody3D.rotation = Other.Rigidbody3D.rotation;
            }
        }

        public void Rename(string name)
        {
            gameObject.name = name;
            Other.gameObject.name = name;
        }

        internal void Free()
        {
            if (Rigidbody3D)
            {
                Rigidbody3D.isKinematic = Other.Rigidbody3D.isKinematic;
            }
            if (Rigidbody2D)
            {
                Rigidbody2D.isKinematic = Other.Rigidbody2D.isKinematic;
            }
        }
        internal void Freeze()
        {
            if (Rigidbody3D)
            {
                Rigidbody3D.isKinematic = true;
            }
            if (Rigidbody2D)
            {
                Rigidbody2D.isKinematic = true;
            }
        }

        protected virtual void OnEnable()
        {
            if (IsOriginal) if (Other) Other.Active = true;
        }
        protected virtual void OnDisable()
        {
            if (IsOriginal) if (Other) Other.Active = false;
        }

        protected virtual void OnDestroy()
        {
            if (IsOriginal) PredictionSystem.Objects.Remove(this);
        }
    }
}