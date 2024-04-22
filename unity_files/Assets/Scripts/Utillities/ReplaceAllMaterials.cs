using UnityEngine;

public class ApplyMaterialRecursively : MonoBehaviour
{
    public Material newMaterial;

    void OnValidate()
    {
        ApplyMaterial(this.gameObject);
    }

    void ApplyMaterial(GameObject obj)
    {
        foreach (Transform child in obj.transform)
        {
            // Apply the material to the current object if it has a MeshRenderer
            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // Create an array of materials the same size as the current materials array
                // and fill it with the new material
                Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = newMaterial;
                }
                renderer.materials = newMaterials;
            }

            // Recursively apply to children
            ApplyMaterial(child.gameObject);
        }
    }
}
