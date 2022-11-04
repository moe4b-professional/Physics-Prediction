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

        public int Snapshots => Coordinates.Count;

        [field: NonSerialized]
        public new PredictionRecorder Other { get; internal set; }

        protected override void Awake()
        {
            base.Awake();

            Other = base.Other as PredictionRecorder;

            if (IsOriginal)
            {
                Coordinates = Other.Coordinates;
            }
            else
            {
                Coordinates = ListPool<PredictionCoordinate>.Get();
            }
        }

        bool Block;

        internal void Begin()
        {
            Block = false;

            Coordinates.Clear();
        }
        internal void Capture()
        {
            if (Block) return;

            var coordinate = new PredictionCoordinate(Position, Rotation);
            Coordinates.Add(coordinate);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if(IsClone)
                ListPool<PredictionCoordinate>.Release(Coordinates);
        }
    }
}