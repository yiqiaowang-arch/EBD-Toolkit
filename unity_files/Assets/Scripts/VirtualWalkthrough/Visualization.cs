using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

namespace EBD
{
    public class Visualization
    {

        public static void RenderTrajectory(
            LineRenderer lineRenderer,
            List<Vector3> positions,
            List<float> timesteps,
            float currentTimeStep,
            float trajectoryWidth,
            Gradient gradient = null,
            Color color = default,
            bool normalizeTime = false,
            bool normalizePosition = false
        )
        {
            // Normalize time steps.
            if (normalizeTime)
            {
                float min = timesteps.Min();
                float max = timesteps.Max();
                float new_range = max - min;
                for (int i = 0; i < timesteps.Count; i++)
                {
                    timesteps[i] = (timesteps[i] - min) / new_range;
                }
            }

            // Normalize positions.
            if (normalizePosition)
            {
                float minX = positions.Min(p => p.x);
                float maxX = positions.Max(p => p.x);
                float minY = positions.Min(p => p.y);
                float maxY = positions.Max(p => p.y);
                float minZ = positions.Min(p => p.z);
                float maxZ = positions.Max(p => p.z);
                float new_rangeX = maxX - minX;
                float new_rangeY = maxY - minY;
                float new_rangeZ = maxZ - minZ;
                for (int i = 0; i < positions.Count; i++)
                {
                    positions[i] = new Vector3(
                        (positions[i].x - minX) / new_rangeX,
                        (positions[i].y - minY) / new_rangeY,
                        (positions[i].z - minZ) / new_rangeZ
                    );
                }
            }

            // Calculate the number of points to render.
            int numPoints = 0;
            for (int i = 0; i < timesteps.Count; i++)
            {
                if (timesteps[i] <= currentTimeStep)
                {
                    numPoints++;
                }
            }

            // If the gradient is not provided, construct gradient from color.
            if (gradient == null)
            {
                gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                );
            }
            lineRenderer.colorGradient = gradient;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));  // Default material for linerenderer.
            lineRenderer.widthMultiplier = trajectoryWidth;
            lineRenderer.positionCount = numPoints;
            lineRenderer.SetPositions(positions.GetRange(0, numPoints).ToArray());
        }

        public static void SetupParticleSystem(
            ParticleSystem particleSystem,
            List<Vector3> particlePositions,
            List<float> heatmapValues,
            Gradient heatmapGradient,
            float particleSize)
        {
            var partSysMain = particleSystem.main;
            partSysMain.maxParticles = particlePositions.Count;
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particlePositions.Count];
            particleSystem.GetParticles(particles, particles.Length, 0);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].position = particlePositions[i];
                particles[i].velocity = Vector3.zero;
                particles[i].startSize = particleSize;
                particles[i].startColor = heatmapGradient.Evaluate(heatmapValues[i]);
            }
            particleSystem.SetParticles(particles, particles.Length, 0);
        }

        public static void SetupParticleSystem(
            ParticleSystem particleSystem,
            List<Vector3> particlePositions,
            List<Color> particleColors,
            float particleSize
        )
        {
            var partSysMain = particleSystem.main;
            partSysMain.maxParticles = particlePositions.Count;
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particlePositions.Count];
            particleSystem.GetParticles(particles, particles.Length, 0);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].position = particlePositions[i];
                particles[i].velocity = Vector3.zero;
                particles[i].startSize = particleSize;
                particles[i].startColor = particleColors[i];
            }
            particleSystem.SetParticles(particles, particles.Length, 0);
        }

        public static void AddTMProLabel(
            string titleText,
            float fontSize,
            Vector3 constantDisplacement,
            Vector3 displacement)
        {
            GameObject titleTextGO = new GameObject("LabelText");
            titleTextGO.hideFlags = HideFlags.HideInHierarchy;
            TMPro.TextMeshPro textMesh = titleTextGO.AddComponent<TMPro.TextMeshPro>();
            textMesh.text = titleText;
            textMesh.fontSize = fontSize;
            textMesh.transform.position = constantDisplacement + displacement;
            textMesh.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            textMesh.enableWordWrapping = false;
            textMesh.color = Color.black;
            textMesh.fontStyle = TMPro.FontStyles.Bold;
        }
    }
}
