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
	[RequireComponent(typeof(PredictionObject))]
	public class TrajectoryPredictionDrawer : MonoBehaviour
	{
        [SerializeField]
        LineRenderer line = default;

        public PredictionObject Target { get; protected set; }

        public Vector3[] Points { get; protected set; }

        void Awake()
        {
            Target = GetComponent<PredictionObject>();
        }

        void Start()
        {
            PredictionSystem.OnStart += PredictionStartCallback;
            PredictionSystem.OnEnd += PredictionEndCallback;

            OnHide += HideCallback;
        }

        void HideCallback()
        {
            line.positionCount = 0;
        }

        void PredictionStartCallback()
        {
            Points = PredictionSystem.RecordObject(Target);
        }

        void PredictionEndCallback()
        {
            line.positionCount = Points.Length;
            line.SetPositions(Points);
        }

        void OnDestroy()
        {
            PredictionSystem.OnStart -= PredictionStartCallback;
            PredictionSystem.OnEnd -= PredictionEndCallback;

            OnHide -= HideCallback;
        }

        //Static Utility
        public static event Action OnHide;
        public static void Hide()
        {
            OnHide?.Invoke();
        }
    }
}