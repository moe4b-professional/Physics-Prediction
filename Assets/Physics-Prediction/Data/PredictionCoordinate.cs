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
	public struct PredictionCoordinate
	{
		public Vector3 Position { get; private set; }
		public Quaternion Rotation { get; private set; }

		public PredictionCoordinate(Vector3 position, Quaternion rotation)
		{
			this.Position = position;
			this.Rotation = rotation;
		}

		public static PredictionCoordinate From(Transform transform) => From(transform, Space.World);
		public static PredictionCoordinate From(Transform transform, Space space)
		{
            switch (space)
            {
                case Space.World:
					return new PredictionCoordinate(transform.position, transform.rotation);

                case Space.Self:
					return new PredictionCoordinate(transform.localPosition, transform.localRotation);
            }

			throw new NotImplementedException();
        }
	}
}