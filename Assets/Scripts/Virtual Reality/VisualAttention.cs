using System.Collections.Generic;
using UnityEngine;

public class VisualAttention
{
    public static List<Vector3> CastAndCollide(
        Vector3 srcPos,
        Vector3 forward,
        Vector3 up,
        Vector3 right,
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
}
