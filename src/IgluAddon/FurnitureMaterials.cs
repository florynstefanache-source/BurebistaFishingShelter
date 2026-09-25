using System;
using System.IO;
using Il2Cpp;
using UnityEngine;

namespace BurebistaFishingShelterFeatures
{
    public sealed partial class Main
    {
        private Texture2D fallbackWood;
        private Material FurnitureMaterial(string part, Color fallbackColor)
        {
            string gearName = part == "Lashing" ? "GEAR_GutDried" : part.StartsWith("Rack") ? "GEAR_Stick" : "GEAR_ReclaimedWoodB";
            foreach (Material cached in furnitureMaterials)
                if (cached != null && cached.name == "BurebistaFurniture_" + gearName) return cached;
            GearItem prefab = GearItem.LoadGearItemPrefab(gearName);
            Material result = null;
            if (prefab != null)
                foreach (Renderer source in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in source.sharedMaterials)
                    {
                        if (material == null || material.shader == null || material.mainTexture == null) continue;
                        // Clone the game's textured material; never edit the prefab's material.
                        result = new Material(material);
                        break;
                    }
                    if (result != null) break;
                }
            if (result == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
                if (shader == null) throw new InvalidOperationException("No hay shader iluminado disponible");
                result = new Material(shader);
                string path = Path.Combine(StateDirectory, "iceland", "wood05.jpg");
                if (fallbackWood == null && File.Exists(path))
                {
                    fallbackWood = new Texture2D(2, 2);
                    if (!ImageConversion.LoadImage(fallbackWood, File.ReadAllBytes(path)))
                    { UnityEngine.Object.Destroy(fallbackWood); fallbackWood = null; }
                }
                result.mainTexture = fallbackWood;
                result.color = fallbackWood == null ? fallbackColor : new Color(.72f,.68f,.60f,1f);
                MelonLoader.MelonLogger.Warning("Textura de respaldo para " + gearName);
            }
            result.name = "BurebistaFurniture_" + gearName;
            if (result.HasProperty("_Glossiness")) result.SetFloat("_Glossiness", .05f);
            if (result.HasProperty("_Metallic")) result.SetFloat("_Metallic", 0f);
            if (result.HasProperty("_EmissionColor")) result.SetColor("_EmissionColor", Color.black);
            result.DisableKeyword("_EMISSION");
            furnitureMaterials.Add(result);
            return result;
        }
    }
}





