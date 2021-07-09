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

namespace Default
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExectionOrder)]
#pragma warning disable CS0108
    public class PredictionObject : MonoBehaviour, IPredictionPersistantObject
    {
        public const int ExectionOrder = -200;

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
        public Vector3 LocalScale
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        public bool Active
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public Rigidbody rigidbody { get; protected set; }
        public bool HasRigidbody => rigidbody != null;

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

        void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();

            IsClone = PredictionSystem.CloneFlag;
            IsOriginal = !IsClone;

            if (IsOriginal) PredictionSystem.AddObject(this);
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
            if (IsOriginal) PredictionSystem.RemoveObject(this);
        }
    }
#pragma warning restore CS0108
}