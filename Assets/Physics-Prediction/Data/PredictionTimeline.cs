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
    public class PredictionTimeline : List<PredictionCoordinate>
    {
        public virtual void Add(Vector3 position, Quaternion rotation)
        {
            var coordinate = new PredictionCoordinate(position, rotation);

            Add(coordinate);
        }
    }
}