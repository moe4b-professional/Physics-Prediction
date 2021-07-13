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
	public class ProjectileShooter2D : MonoBehaviour
	{
		[SerializeField]
		GameObject prefab = default;

		[SerializeField]
		ForceData force = new ForceData(Vector2.right * 50 + Vector2.up * 3, ForceMode2D.Impulse);
		[Serializable]
		public struct ForceData
        {
			[SerializeField]
            Vector2 vector;
            public Vector2 Vector => vector;

			[SerializeField]
            ForceMode2D mode;
            public ForceMode2D Mode => mode;

            public ForceData(Vector2 vector, ForceMode2D mode)
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
        }

		PredictionTimeline timeline;

		public const KeyCode Key = KeyCode.Mouse0;

		Transform InstanceContainer;

        void Start()
        {
            InstanceContainer = new GameObject("Projectiles Container").transform;

			StartCoroutine(Procedure());
		}

        void Update()
        {
			LookAtMouse();

			if(Input.GetKeyDown(Key))
				timeline = PredictionSystem.Record.Prefabs.Add(prefab, Launch);

			if (Input.GetKeyUp(Key))
				PredictionSystem.Record.Prefabs.Remove(timeline);

			Shoot();
		}

        void LookAtMouse()
        {
			var point = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);

			var direction = (point - transform.position);

			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
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
			var rigidbody = gameObject.GetComponent<Rigidbody2D>();

			var relativeForce = transform.TransformVector(force.Vector);

			rigidbody.AddForce(relativeForce, force.Mode);

			rigidbody.transform.position = transform.position;
			rigidbody.transform.rotation = transform.rotation;
		}

		IEnumerator Procedure()
        {
			while(true)
            {
				yield return new WaitForSeconds(1f / prediction.Rate);

				Predict();
            }
        }

		void Predict()
        {
			if (Input.GetKey(Key))
			{
				PredictionSystem.Simulate(prediction.Iterations);

				TrajectoryPredictionDrawer.ShowAll();

				prediction.Line.positionCount = timeline.Count;

				for (int i = 0; i < timeline.Count; i++)
					prediction.Line.SetPosition(i, timeline[i].Position);
			}
		}
    }
}