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
	public class Sandbox : MonoBehaviour
	{
        void Start()
        {
            
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}