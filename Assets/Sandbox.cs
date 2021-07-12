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
#if UNITY_EDITOR
		[MenuItem("Sandbox/Execute")]
		public static void Execute()
        {
			var material = new PhysicsMaterial2D();

			AssetDatabase.CreateAsset(material, "Assets/Material.physicsMaterial2D");
        }
#endif
	}
}