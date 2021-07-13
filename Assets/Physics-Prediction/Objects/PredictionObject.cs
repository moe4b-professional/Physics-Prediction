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

namespace MB.PhysicsPrediction
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExectionOrder)]
    [AddComponentMenu(PredictionSystem.Path + "Prediction Object")]
#pragma warning disable CS0108
    public class PredictionObject : MonoBehaviour, IPredictionPersistantObject
    {
        public const int ExectionOrder = -200;

        [SerializeField]
        PredictionPhysicsMode mode = PredictionPhysicsMode.Physics3D;
        public PredictionPhysicsMode Mode => mode;

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

        public bool Active
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public Rigidbody rigidbody { get; protected set; }
        public bool HasRigidbody => rigidbody != null;

        public Rigidbody2D rigidbody2D { get; protected set; }
        public bool HasRigidbody2D => rigidbody2D != null;

        public bool IsClone { get; protected set; }
        public PredictionObject Original
        {
            get
            {
                if (IsOriginal) throw new Exception("Current Object is Already The Original");

                return Other;
            }
        }

        public bool IsOriginal { get; protected set; }
        public PredictionObject Clone
        {
            get
            {
                if (IsClone) throw new Exception("Current Object is Already The Clone");

                return Other;
            }
        }

        public PredictionObject Other { get; internal set; }
        
        void Reset()
        {
            mode = PredictionSystem.CheckPhysicsMode(gameObject, mode);
        }

        void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
            rigidbody2D = GetComponent<Rigidbody2D>();

            IsClone = PredictionSystem.Clone.Flag;
            IsOriginal = !IsClone;

            if (IsOriginal) PredictionSystem.Objects.Add(this, mode);
        }

        void OnEnable()
        {
            if (IsOriginal) Other.Active = true;
        }

        void OnDisable()
        {
            if (IsOriginal) if(Other) Other.Active = false;
        }

        void OnDestroy()
        {
            if (IsOriginal) PredictionSystem.Objects.Remove(this);
        }
    }
#pragma warning restore CS0108
}