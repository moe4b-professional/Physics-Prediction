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
	public class ProjectileShooter3D : MonoBehaviour
	{
		[SerializeField]
		GameObject prefab = default;

		[SerializeField]
		ForceData force = new ForceData(Vector3.forward * 50 + Vector3.up * 3, ForceMode.VelocityChange);
		[Serializable]
		public struct ForceData
        {
			[SerializeField]
            Vector3 vector;
            public Vector3 Vector => vector;

			[SerializeField]
            ForceMode mode;
            public ForceMode Mode => mode;

            public ForceData(Vector2 vector, ForceMode mode)
            {
				this.vector = vector;
				this.mode = mode;
            }
        }

		[SerializeField]
		PredictionProperty prediction = default;
		[Serializable]
		public class PredictionProperty
        {
			[SerializeField]
			int iterations = 40;
			public int Iterations => iterations;

			[SerializeField]
			int rate = 30;
			public int Rate => rate;

			[SerializeField]
            LineRenderer line = default;
            public LineRenderer Line => line;

			internal PredictionRecorder Target;
        }

		public const KeyCode Key = KeyCode.Mouse0;

		Transform InstanceContainer;

        void Start()
        {
            InstanceContainer = new GameObject("Projectiles Container").transform;

            prediction.Target = PredictionSystem.Prefabs.Add(prefab, Launch);

            StartCoroutine(Procedure());
		}

        void Update()
        {
			LookAtMouse();

			Shoot();
		}

        void LookAtMouse()
        {
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			if(Physics.Raycast(ray, out var hit))
            {
				var direction = hit.point - transform.position;

				var target = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 240f * Time.deltaTime);
			}
        }

		void Shoot()
		{
			if (Input.GetKeyUp(Key))
			{
				prediction.Line.positionCount = 0;

				var instance = Instantiate(prefab);
				instance.transform.SetParent(InstanceContainer);
				Launch(instance);

				TrajectoryPredictionDrawer.HideAll();
			}
		}

		void Launch(GameObject gameObject)
		{
			var rigidbody = gameObject.GetComponent<Rigidbody>();

			var relativeForce = transform.TransformVector(force.Vector);

			rigidbody.AddForce(relativeForce, force.Mode);

			rigidbody.transform.position = transform.position;
			rigidbody.transform.rotation = transform.rotation;
		}

        IEnumerator Procedure()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f / prediction.Rate);

                if (Input.GetKey(Key) == false) continue;

                PredictionSystem.Simulate(prediction.Iterations);

                TrajectoryPredictionDrawer.ShowAll();

                prediction.Line.positionCount = prediction.Target.Snapshots;

                for (int i = 0; i < prediction.Target.Snapshots; i++)
                    prediction.Line.SetPosition(i, prediction.Target.Coordinates[i].Position);
            }
        }

        void OnDestroy()
        {
            PredictionSystem.Prefabs.Remove(prediction.Target);
        }
    }
}