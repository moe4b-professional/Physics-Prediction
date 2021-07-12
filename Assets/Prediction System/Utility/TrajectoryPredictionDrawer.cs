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

        PredictionSystem.Timeline timeline;

        void Awake()
        {
            Target = GetComponent<PredictionObject>();
        }

        void Start()
        {
            timeline = PredictionSystem.Record.Objects.Add(Target);

            PredictionSystem.OnSimulate += PredictionSimulateCallback;

            OnShowAll += Show;
            OnHideAll += Hide;
        }

        void PredictionSimulateCallback(int iterations)
        {
            line.positionCount = timeline.Count;

            for (int i = 0; i < timeline.Count; i++)
                line.SetPosition(i, timeline[i]);
        }

        #region Visibility
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
        #endregion

        void OnDestroy()
        {
            PredictionSystem.Record.Objects.Remove(Target);

            PredictionSystem.OnSimulate -= PredictionSimulateCallback;

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