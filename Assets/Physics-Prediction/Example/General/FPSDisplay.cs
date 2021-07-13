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
	public class FPSDisplay : MonoBehaviour
	{
        [SerializeField]
        int size = 100;

        [SerializeField]
        float pollDelay = 0.2f;

        [SerializeField]
        int maxSamples = 20;

        Queue<int> samples = new Queue<int>();

        void Start()
        {
            StartCoroutine(Poll());
        }

        IEnumerator Poll()
        {
            while (true)
            {
                var sample = Mathf.RoundToInt(1 / Time.unscaledDeltaTime);

                Debug.Log(sample);

                samples.Enqueue(sample);

                while (samples.Count > maxSamples) samples.Dequeue();

                yield return new WaitForSecondsRealtime(pollDelay);
            }
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = size;

            var average = (int)samples.Average();

            GUILayout.Label($"FPS: {average}", style);
        }
	}
}