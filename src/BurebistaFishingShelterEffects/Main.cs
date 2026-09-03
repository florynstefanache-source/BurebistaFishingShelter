using MelonLoader;
using UnityEngine;
using Il2Cpp;

[assembly: MelonInfo(typeof(BurebistaFishingShelterEffects.Main), "Burebista Fishing Shelter Effects", "0.5.0", "Burebista")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace BurebistaFishingShelterEffects
{
    public sealed class Main : MelonMod
    {
        private GameObject attachedShelter;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("+15 C and indoor weather protection loaded.");
        }

        public override void OnUpdate()
        {
            GameObject shelter = GameObject.Find("BurebistaFishingShelter");
            if (shelter == null)
                return;

            if (shelter != attachedShelter)
            {
                attachedShelter = shelter;
                AddInteriorVolume(shelter);
            }

            // Comprueba también la puerta, porque su malla cambia al abrir/cerrar.
            FixDoubleSidedMeshes(shelter);
        }

        public override void OnLateUpdate()
        {
            if (attachedShelter != null)
                ForceOpaqueMaterials(attachedShelter);
        }

        private static void ForceOpaqueMaterials(GameObject shelter)
        {
            Shader opaqueShader = Shader.Find("Unlit/Texture");
            foreach (Renderer renderer in shelter.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                foreach (Material material in renderer.materials)
                {
                    if (material == null)
                        continue;

                    if (opaqueShader != null)
                        material.shader = opaqueShader;

                    Color color = material.color;
                    color.a = 1f;
                    material.color = color;
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetInt("_Mode", 0);
                    material.SetInt("_Surface", 0);
                    material.SetInt("_SrcBlend", 1);
                    material.SetInt("_DstBlend", 0);
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_AlphaClip", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 2000;
                }
            }
        }

        private static void FixDoubleSidedMeshes(GameObject shelter)
        {
            foreach (MeshFilter filter in shelter.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null || filter.gameObject.name.EndsWith("_BurebistaDoubleSided"))
                    continue;

                Mesh mesh = filter.mesh;
                var original = mesh.triangles;
                int count = original.Length;
                int[] doubled = new int[count * 2];

                for (int i = 0; i < count; i++)
                    doubled[i] = original[i];

                for (int i = 0; i + 2 < count; i += 3)
                {
                    doubled[count + i] = original[i];
                    doubled[count + i + 1] = original[i + 2];
                    doubled[count + i + 2] = original[i + 1];
                }

                mesh.triangles = doubled;
                mesh.RecalculateBounds();
                filter.gameObject.name += "_BurebistaDoubleSided";
            }

            ForceOpaqueMaterials(shelter);
        }

        private static void AddInteriorVolume(GameObject shelter)
        {
            GameObject volume = new GameObject("BurebistaFishingShelterProtection");
            volume.transform.SetParent(shelter.transform, false);
            volume.transform.localPosition = new Vector3(0f, 10f, 0f);

            BoxCollider trigger = volume.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.size = new Vector3(42f, 30f, 42f);

            HeatSource heat = volume.AddComponent<HeatSource>();
            heat.m_MaxTempIncrease = 15f;
            heat.m_MaxTempIncreaseInnerRadius = 2.2f;
            heat.m_MaxTempIncreaseOuterRadius = 2.6f;
            heat.m_TimeToReachMaxTempMinutes = 0f;
            heat.m_StartingTemp = 15f;
            heat.m_StartOn = true;
            heat.TurnOn();

            WindKiller wind = volume.AddComponent<WindKiller>();
            wind.m_Collider = trigger;

            ParticleKiller snow = volume.AddComponent<ParticleKiller>();
            snow.m_KillsFallingSnow = true;
            snow.m_KillsBlowingSnow = true;

            AddOpaqueInnerShell(shelter);
            MelonLogger.Msg("[FishingShelter] +15 C, viento y nieve bloqueados; zona exterior utilizable.");
        }

        private static void AddOpaqueInnerShell(GameObject shelter)
        {
            Texture texture = null;
            foreach (Renderer source in shelter.GetComponentsInChildren<Renderer>(true))
            {
                if (source == null || source.material == null || source.material.mainTexture == null)
                    continue;

                string textureName = source.material.mainTexture.name.ToLowerInvariant();
                if (textureName.Contains("snow") || textureName.Contains("iglu"))
                {
                    texture = source.material.mainTexture;
                    break;
                }
            }

            CreateShellPanel(shelter, "InnerBack", new Vector3(0f, 10f, 18f), new Vector3(36f, 22f, 0.8f), texture);
            CreateShellPanel(shelter, "InnerLeft", new Vector3(-18f, 10f, 0f), new Vector3(0.8f, 22f, 36f), texture);
            CreateShellPanel(shelter, "InnerRight", new Vector3(18f, 10f, 0f), new Vector3(0.8f, 22f, 36f), texture);
            CreateShellPanel(shelter, "InnerRoof", new Vector3(0f, 21f, 0f), new Vector3(36f, 0.8f, 36f), texture);
            CreateShellPanel(shelter, "InnerFrontLeft", new Vector3(-12f, 10f, -18f), new Vector3(12f, 22f, 0.8f), texture);
            CreateShellPanel(shelter, "InnerFrontRight", new Vector3(12f, 10f, -18f), new Vector3(12f, 22f, 0.8f), texture);
            CreateShellPanel(shelter, "InnerFrontTop", new Vector3(0f, 19f, -18f), new Vector3(12f, 4f, 0.8f), texture);
        }

        private static void CreateShellPanel(GameObject shelter, string name, Vector3 position, Vector3 scale, Texture texture)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = name;
            panel.transform.SetParent(shelter.transform, false);
            panel.transform.localPosition = position;
            panel.transform.localScale = scale;

            Collider collider = panel.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            Renderer renderer = panel.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Unlit/Texture"));
            material.name = "BurebistaOpaqueIceShell";
            material.mainTexture = texture;
            renderer.material = material;
        }
    }
}
