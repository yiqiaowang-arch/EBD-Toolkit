using System.Collections.Generic;
using UnityEngine;
using Trajectory = System.Collections.Generic.List<EBD.TrajectoryEntry>;

namespace EBD
{
    public class VisualAttention
    {
        public static List<Vector3> CastAndCollide(
            Vector3 srcPos,
            Vector3 forward,
            float radiusVertical,
            float radiusHorizontal,
            int numRaysPerRaycast,
            LayerMask layerMask,
            ref int[] hitsPerLayer)
        {
            Vector3 hitPos = Vector3.zero;
            List<Vector3> results = new List<Vector3>();
            for (int i = 0; i < numRaysPerRaycast; i++)
            {
                Vector3 p = SamplePointsWithinCone(srcPos, forward, radiusVertical, radiusHorizontal);
                if (IsCollision(srcPos, p - srcPos, layerMask, ref hitPos, ref hitsPerLayer))
                {
                    results.Add(hitPos);
                }
            }

            return results;
        }

        static Vector2 SamplePointsWithinEllipse(float minorAxis, float majorAxis)
        {
            float angle = UnityEngine.Random.value * 2.0f * Mathf.PI;
            float r = Mathf.Sqrt(UnityEngine.Random.value);
            return new Vector2(majorAxis * r * Mathf.Cos(angle), minorAxis * r * Mathf.Sin(angle));
        }

        static Vector3 SamplePointsWithinCone(Vector3 srcPos, Vector3 axis, float minorAxis, float majorAxis)
        {
            Vector2 p = SamplePointsWithinEllipse(minorAxis, majorAxis);
            return srcPos + axis + p.x * Vector3.up + p.y * Vector3.right;
        }

        static bool IsCollision(
            Vector3 start,
            Vector3 dir,
            LayerMask layerMask,
            ref Vector3 hitPos,
            ref int[] hitsPerLayer)
        {
            // If a hit occurs, this will hold all the information about it.
            RaycastHit hit;

            // Casting the ray and checking for collision.
            if (!Physics.Raycast(start, dir, out hit))
            {
                return false;
            }


            // The MeshCollider the ray hit. NULL-check.
            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
            {
                return false;
            }

            // Check if it is in the desired layer.
            if (!(layerMask == (layerMask | (1 << meshCollider.gameObject.layer))))
            {
                return false;
            }
            hitsPerLayer[meshCollider.gameObject.layer] += 1;
            hitPos = hit.point;
            return true;
        }

        public static (List<Vector3>, List<float>, Dictionary<string, int[]>) CreateHeatMap(
            Dictionary<string, Trajectory> trajectories,
            int maxNumRays,
            float outerConeRadiusVertical,
            float outerConeRadiusHorizontal,
            int numRaysPerRayCast,
            LayerMask layerMask,
            float kernelSize
        )
        {
            // Subsample the trajectory.

            // Get the total number of positions.
            int totalNumPositions = 0;
            foreach (KeyValuePair<string, Trajectory> entry in trajectories)
            {
                totalNumPositions += entry.Value.Count;
            }

            // This is not stricly uniform sampling, but we need to ensure that
            // we perform at least one ray cast per trajectory to have
            // data for the statistics.
            List<Vector3> hitPositions = new();
            Dictionary<string, int[]> hitsPerLayer = new();
            foreach (KeyValuePair<string, Trajectory> entry in trajectories)
            {
                float trajectoryProportion = (float)entry.Value.Count / totalNumPositions;
                int currMaxNumRays = Mathf.CeilToInt(trajectoryProportion * maxNumRays);
                Debug.Log($"currMaxNumRays={currMaxNumRays}");
                int currNumRays = 0;
                int[] currHitsPerLayer = new int[32];
                int loopIndex = 0;

                // This is the lowest number of samples we can take.
                // This only occurs if all rays always hit.
                int lowerBoundSamples = currMaxNumRays / numRaysPerRayCast;
                while (currNumRays < currMaxNumRays)
                {
                    if (loopIndex >= 10 * lowerBoundSamples)
                    {
                        // This is a safety check to avoid infinite loops.
                        Debug.LogWarning("Could not sample enough points for the heatmap.");
                        break;
                    }

                    int index = Random.Range(0, entry.Value.Count);
                    TrajectoryEntry trajectoryEntry = entry.Value[index];
                    List<Vector3> hits = CastAndCollide(
                        trajectoryEntry.Position,
                        trajectoryEntry.ForwardDirection,
                        outerConeRadiusVertical,
                        outerConeRadiusHorizontal,
                        numRaysPerRayCast,
                        layerMask,
                        ref currHitsPerLayer
                    );
                    hitPositions.AddRange(hits);
                    currNumRays += hits.Count;
                    loopIndex++;
                }
                hitsPerLayer[entry.Key] = currHitsPerLayer;
            }

            List<float> kdeValues = KernelDensityEstimate.Evaluate(hitPositions, hitPositions, kernelSize);
            Debug.Log($"Number of hit positions: {hitPositions.Count}");
            return (hitPositions, kdeValues, hitsPerLayer);
        }
    }
}
