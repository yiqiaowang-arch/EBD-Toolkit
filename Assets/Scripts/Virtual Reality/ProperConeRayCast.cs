/*
DesignMind: A Toolkit for Evidence-Based, Cognitively- Informed and Human-Centered Architectural Design
Copyright (C) 2023  michal Gath-Morad, Christoph Hölscher, Raphaël Baur

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>
*/

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
