using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MB.PhysicsPrediction
{
    [AddComponentMenu(PredictionSystem.Path + "Utility/" + "Prediction Blocker")]
	public class PredictionBlocker : MonoBehaviour, IPredictionPersistantObject
	{
		public bool IsClone { get; set; }

#if UNITY_EDITOR
        void Reset()
		{
			if(TryGetComponent<PredictionObject>(out var target) == false)
			{
				const string Text = "Prediction Blocker Requires a Prediction Object Component Variant Attached, Either a Prediction Obstacle or a Prediction Recorder, Press Ok to Add Prediction Obstacle Component";

                if(EditorUtility.DisplayDialog("Invalid Component Setup", Text, "Ok", "Cancel"))
				{
					Undo.AddComponent<PredictionObstacle>(gameObject);
				}
				else
				{
                    DestroyImmediate(this);
                }
            }
		}
#endif
	}
}