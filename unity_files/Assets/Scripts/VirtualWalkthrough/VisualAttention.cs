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

        private void CreateHeatMap()
        {
            // Subsample the trajectory.

            // Get the total number of positions.
            int totalNumPositions = 0;
            foreach (KeyValuePair<string, Trajectory> entry in trajectories)
            {
                totalNumPositions += entry.Value.Count;
            }

            // This is not stricly uniform sampling, but to ensure that each 
            // trajectory is in the sample we will make sure that there is at least
            // one sample from each trajectory.
            Dictionary<string, int> numSamplesPerTrajectory = new();
            foreach (KeyValuePair<string, Trajectory> entry in trajectories)
            {
                float trajectoryProportion = (float)entry.Value.Count / totalNumPositions;
                int numSamples = Mathf.CeilToInt(trajectoryProportion * numRayCast);
                numSamplesPerTrajectory.Add(entry.Key, numSamples);
            }

            // Sample `maxNumRays` positions uniformly (with replacement) from all the trajectories.
            Dictionary<string, Trajectory> subsampledTrajectories = new();
            foreach (KeyValuePair<string, Trajectory> entry in trajectories)
            {
                subsampledTrajectories.Add(entry.Key, new Trajectory());
                for (int i = 0; i < numSamplesPerTrajectory[entry.Key]; i++)
                {
                    int index = UnityEngine.Random.Range(0, entry.Value.Count);
                    subsampledTrajectories[entry.Key].Add(trajectories[entry.Key][index]);
                }
            }

            // Unity generates 32 layers per default.
            hitsPerLayer = new();
            foreach (KeyValuePair<string, Trajectory> entry in subsampledTrajectories)
            {
                // Compute hit positions for each trajectory.
                int[] currHitsPerLayer = new int[32];
                hitPositions.AddRange(
                    ComputeHitPositions(
                        entry.Value,
                        outerConeRadiusVertical,
                        outerConeRadiusHorizontal,
                        numRaysPerRayCast,
                        layerMask,
                        ref currHitsPerLayer
                        )
                    );
                hitsPerLayer[entry.Key] = currHitsPerLayer;
            }

            kdeValues = KernelDensityEstimate.Evaluate(hitPositions, hitPositions, kernelSize);
            particlePositions = hitPositions.ToArray();
            Debug.Log($"Number of hit positions: {hitPositions.Count}");
        }
    }
}
