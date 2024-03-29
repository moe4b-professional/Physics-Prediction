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
    [AddComponentMenu(PredictionSystem.Path + "Utility/" + "Trajectory Prediction Drawer")]
	public class TrajectoryPredictionDrawer : MonoBehaviour
    {
        [SerializeField]
        PredictionRecorder target;

        [SerializeField]
        LineRenderer line = default;

        public bool IsClone { get; protected set; }

        void Start()
        {
            line.useWorldSpace = true;

            PredictionSystem.Simulation.OnEnd += PredictionSimulateEndCallback;

            OnShowAll += Show;
            OnHideAll += Hide;
        }

        void PredictionSimulateEndCallback()
        {
            line.positionCount = target.Coordinates.Count;

            for (int i = 0; i < target.Coordinates.Count; i++)
                line.SetPosition(i, target.Coordinates[i].Position);
        }

        public bool Visibile
        {
            get => line.enabled;
            set
            {
                if (value)
                    Show();
                else
                    Hide();
            }
        }

        public void Hide()
        {
            line.enabled = false;
        }
        public void Show()
        {
            line.enabled = true;
        }

        void OnDestroy()
        {
            if (IsClone) return;

            PredictionSystem.Simulation.OnEnd -= PredictionSimulateEndCallback;

            OnShowAll -= Show;
            OnHideAll -= Hide;
        }

        //Static Utility
        public static event Action OnShowAll;
        public static void ShowAll()
        {
            OnShowAll?.Invoke();
        }

        public static event Action OnHideAll;
        public static void HideAll()
        {
            OnHideAll?.Invoke();
        }
    }
}