using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProperConeRayCast : MonoBehaviour
{
    public float angleHorizontal = 90.0f;
    public float angleVertical = 90.0f;
    public int subConeCount = 10;
    public float decay = 1.0f;
    public Gradient gradient;

    // Start is called before the first frame update
    void Start()
    {
        // Horizontal and vertical radii of ellipse one unit displaced from view-point.
        float r_h = Mathf.Tan((angleHorizontal / 2.0f) * Mathf.Deg2Rad);
        float r_v = Mathf.Tan((angleVertical / 2.0f) * Mathf.Deg2Rad);
        float subCone_a = 1.0f / subConeCount;

        // Trace this ellipse with Debug.Lines.
        for (int j = 1; j < subConeCount; j++)
        {
            int p_count = 360;
            Vector3[] points = new Vector3[p_count];
            float a_per_p = 360.0f / p_count;
            for (int i = 0; i < p_count; i++)
            {
                points[i] = transform.position 
                + transform.forward 
                + transform.right * Mathf.Pow(subCone_a * j, decay) * r_h * Mathf.Cos(a_per_p * i * Mathf.Deg2Rad) 
                + transform.up * Mathf.Pow(subCone_a * j, decay) *  r_v * Mathf.Sin(a_per_p * i * Mathf.Deg2Rad);
            }

            Debug.DrawRay(transform.position, transform.forward, Color.green, 120.0f);
            for (int i = 0; i <= p_count; i++)
            {
                Debug.DrawLine(points[i % p_count], points[(i + 1) % 360], gradient.Evaluate(Mathf.Pow(subCone_a * j, decay)), 120.0f);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
