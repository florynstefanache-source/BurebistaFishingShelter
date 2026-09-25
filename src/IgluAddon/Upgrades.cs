using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace BurebistaFishingShelterFeatures
{
    public sealed partial class Main
    {
        private WorkBench workbench;
        private GameObject curingRack;
        private bool hasWorkbench, hasRack;
        private readonly List<Material> furnitureMaterials = new List<Material>();
        private static readonly string[] BenchMaterials = { "GEAR_ReclaimedWoodB", "GEAR_ScrapMetal", "GEAR_Cloth" };
        private static readonly int[] BenchCounts = { 6, 4, 2 };
        private static readonly string[] RackMaterials = { "GEAR_Stick", "GEAR_GutDried", "GEAR_Cloth" };
        private static readonly int[] RackCounts = { 12, 4, 2 };

        private bool InsideShelter()
        {
            Transform player = GameManager.GetPlayerTransform();
            if (root == null || player == null) return false;
            Vector3 p = root.transform.InverseTransformPoint(player.position);
            return Mathf.Abs(p.x) < 20f && p.z > -20f && p.z < 20f && p.y > -3f && p.y < 30f;
        }

        private void HandleUpgrades()
        {
            if (!canInput || !InsideShelter()) return;
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!control && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)
                && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.G)) PlaceNearbyOnRack();
            else if (control && Input.GetKeyDown(KeyCode.J)) BuildUpgrade(true);
            else if (control && Input.GetKeyDown(KeyCode.K)) BuildUpgrade(false);
            else if (!control && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)
                && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)
                && Input.GetKeyDown(KeyCode.T) && workbench != null)
            {
                foreach (Panel_Crafting panel in Resources.FindObjectsOfTypeAll<Panel_Crafting>())
                {
                    if (panel == null || !panel.gameObject.scene.IsValid()) continue;
                    panel.EnableCraftingAtLocation(workbench.Cast<CraftingLocationInterface>());
                    return;
                }
                HUDMessage.AddMessage("La interfaz de crafteo no esta disponible todavia.");
            }
        }

        private void BuildUpgrade(bool bench)
        {
            if (bench ? hasWorkbench : hasRack) { HUDMessage.AddMessage("Esta mejora ya esta construida."); return; }
            Inventory inv = GameManager.GetInventoryComponent();
            string[] names = bench ? BenchMaterials : RackMaterials;
            int[] counts = bench ? BenchCounts : RackCounts;
            if (inv == null) return;
            for (int i = 0; i < names.Length; i++)
                if (inv.GetNumGearWithName(names[i]) < counts[i])
                {
                    HUDMessage.AddMessage(bench ? "Mesa: 6 maderas recuperadas, 4 chatarras, 2 telas." : "Bastidor: 12 palos, 4 tripas curadas, 2 telas.");
                    return;
                }
            // Construct inactive first: unavailable shaders/components must not consume materials.
            GameObject upgrade = null;
            int[] before = new int[names.Length];
            for (int i = 0; i < names.Length; i++) before[i] = inv.GetNumGearWithName(names[i]);
            try
            {
                upgrade = CreateUpgrade(bench);
                for (int i = 0; i < names.Length; i++)
                {
                    inv.RemoveGearFromInventory(names[i], counts[i], true);
                    if (inv.GetNumGearWithName(names[i]) != before[i] - counts[i])
                        throw new InvalidOperationException("No se pudo confirmar el pago de " + names[i]);
                }
                if (bench) hasWorkbench = true; else hasRack = true;
                if (!SaveState()) throw new IOExceptionForUpgrade();
                upgrade.SetActive(true);
                HUDMessage.AddMessage(bench ? "Mesa integrada construida. T: trabajar." : "Bastidor construido. Deja materiales cerca y pulsa G para colocarlos.");
            }
            catch (Exception e)
            {
                if (bench) { hasWorkbench = false; workbench = null; } else { hasRack = false; curingRack = null; }
                if (upgrade != null) UnityEngine.Object.Destroy(upgrade);
                // Return only amounts actually removed, including a partial failing removal.
                for (int i = 0; i < names.Length; i++)
                {
                    int removed = Mathf.Clamp(before[i] - inv.GetNumGearWithName(names[i]), 0, counts[i]);
                    if (removed > 0) Refund(GameManager.GetPlayerManagerComponent(), names[i], removed);
                }
                MelonLogger.Error("Mejora cancelada: " + e.Message);
                HUDMessage.AddMessage("No se pudo construir la mejora; materiales devueltos.");
            }
        }
        private sealed class IOExceptionForUpgrade : Exception { public IOExceptionForUpgrade() : base("No se pudo guardar la mejora") {} }

        private GameObject CreateUpgrade(bool bench)
        {
            Transform furniture = root.transform.Find("BurebistaFurniture");
            var g = new GameObject(bench ? "IntegratedWorkbench" : "IntegratedCuringRack");
            g.SetActive(false);
            g.transform.SetParent(furniture, false);
            g.transform.localPosition = bench ? new Vector3(-12f, 0f, -1f) : new Vector3(12f, 0f, -1f);
            try
            {
                Color wood = new Color(.32f, .21f, .12f, 1f);
                if (bench)
                {
                    foreach (float x in new[] {-2.35f, 0f, 2.35f})
                        Cube("WorktopPlank", g.transform, new Vector3(x, 9, 0), new Vector3(2.25f, 1, 15), wood);
                    foreach (float x in new[] {-2.7f, 2.7f}) foreach (float z in new[] {-6f, 6f})
                        Cube("Leg", g.transform, new Vector3(x, 4.2f, z), new Vector3(1, 8.4f, 1), wood);
                    Cube("LowerBrace", g.transform, new Vector3(0, 3f, 0), new Vector3(1, .8f, 13), wood);
                    workbench = g.AddComponent<WorkBench>();
                    workbench.m_CraftingAndRepairTimeModifier = 1f;
                    workbench.m_CraftingAndRepairSkillModifier = 0f;
                    UpdateFurnitureScale();
                }
                else
                {
                    // Metre-sized rustic A-frame, with its long side along the wall.
                    g.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    foreach (float x in new[] {-.84f, .84f})
                    {
                        RackPole("RackLeg", g.transform, new Vector3(x,0,-.3f), new Vector3(x,1.55f,0), .065f, wood);
                        RackPole("RackLeg", g.transform, new Vector3(x,0,.3f), new Vector3(x,1.55f,0), .065f, wood);
                    }
                    RackPole("RackTop", g.transform, new Vector3(-.92f,1.4f,0), new Vector3(.92f,1.4f,0), .07f, wood);
                    foreach (float z in new[] {-.25f,.25f})
                        RackPole("RackBrace", g.transform, new Vector3(-.84f,.45f,z), new Vector3(.84f,.45f,z), .05f, wood);
                    // Close-spaced poles form a usable, collidable drying surface.
                    for (int i = 0; i < 23; i++)
                    {
                        float x = -.77f + i * .07f;
                        RackPole("RackSlat", g.transform, new Vector3(x,.49f,-.3f), new Vector3(x,.49f,.3f), .075f, wood);
                    }
                    foreach (float x in new[] {-.57f,-.19f,.19f,.57f})
                        RackPole("Lashing", g.transform, new Vector3(x,1.4f,0), new Vector3(x,1.14f,0), .009f, new Color(.48f,.42f,.28f,1));
                    curingRack = g;
                    UpdateFurnitureScale();
                }
                return g;
            }
            catch { UnityEngine.Object.Destroy(g); throw; }
        }

        internal void UpdateFurnitureScale()
        {
            UpdateStorageScale();
            if (workbench != null)
            {
                Vector3 parentScale = workbench.transform.parent.lossyScale;
                // The tabletop top is at model y=9.5. Half v1.14.8: cap at 90 cm in world space.
                // Shrink only for the smallest shelters so it still fits beside the wall.
                float target = (.9f / 9.5f) * Mathf.Min(1f, Mathf.Abs(parentScale.x) / .08f);
                workbench.transform.localScale = new Vector3(target / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
                    target / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), target / Mathf.Max(.001f, Mathf.Abs(parentScale.z)));
            }
            if (curingRack == null) return;
            Vector3 scale = curingRack.transform.parent.lossyScale;
            // Retain fit in very small shelters, double the v1.14.7 frame (about 2.18 m including pole ends).
            float fit = 1.38f * Mathf.Min(1f, Mathf.Abs(scale.x) / .08f);
            curingRack.transform.localScale = new Vector3(fit / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                fit / Mathf.Max(.001f, Mathf.Abs(scale.y)), fit / Mathf.Max(.001f, Mathf.Abs(scale.z)));
        }

        private GameObject RackPole(string name, Transform parent, Vector3 a, Vector3 b, float diameter, Color color)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = name;
            pole.transform.SetParent(parent, false);
            pole.transform.localPosition = (a + b) * .5f;
            pole.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            pole.transform.localScale = new Vector3(diameter, (b - a).magnitude * .5f, diameter);
            Renderer renderer = pole.GetComponent<Renderer>();
            renderer.sharedMaterial = FurnitureMaterial(name, color);
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (name == "Lashing") pole.GetComponent<Collider>().enabled = false;
            return pole;
        }

        private static bool RackContains(Vector3 p, bool occupied)
        {
            float margin = occupied ? .15f : 0f;
            return Mathf.Abs(p.x) <= .94f + margin && Mathf.Abs(p.z) <= .4f + margin
                && p.y >= .43f - margin && p.y <= 1.6f + margin;
        }

        internal bool IsOnCuringRack(EvolveItem evolve)
        {
            if (!hasRack || curingRack == null || evolve == null || evolve.m_GearItem == null) return false;
            GearItem gear = evolve.m_GearItem;
            if (gear.m_InPlayerInventory || gear.IsWornOut() || evolve.m_TimeToEvolveGameDays <= 0f) return false;
            Vector3 p = curingRack.transform.InverseTransformPoint(gear.transform.position);
            // Includes shelf surfaces and upper frame, excludes floor, bench and nearby world gear.
            return RackContains(p, false);
        }

        private void DrawUpgrades()
        {
            if (!InsideShelter()) return;
            Inventory inv = GameManager.GetInventoryComponent();
            if (inv == null) return;
            string bench = hasWorkbench ? "T  USAR MESA DE TRABAJO" : "Ctrl+J  MEJORAR: MESA   " + Requirements(inv, BenchMaterials, BenchCounts, new[]{"madera recuperada", "chatarra", "tela"});
            string rack = hasRack ? "G: colocar pieles, tripas y retonos cercanos en el bastidor (6 sitios)" : "Ctrl+K  MEJORAR: BASTIDOR   " + Requirements(inv, RackMaterials, RackCounts, new[]{"palos", "tripas curadas", "tela"});
            GUI.Label(new Rect(10, Screen.height - 310, Screen.width - 20, 30), bench, style);
            GUI.Label(new Rect(10, Screen.height - 280, Screen.width - 20, 30), rack, style);
        }
        private static string Requirements(Inventory inv, string[] names, int[] counts, string[] labels)
        {
            var parts = new string[names.Length];
            for (int i = 0; i < names.Length; i++) parts[i] = labels[i] + " " + inv.GetNumGearWithName(names[i]) + "/" + counts[i];
            return string.Join("   ", parts);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(EvolveItem), nameof(EvolveItem.IsIndoorScene))]
    internal static class RackIndoorPatch
    {
        private static void Postfix(EvolveItem __instance, ref bool __result)
        {
            if (Main.Instance != null && Main.Instance.IsOnCuringRack(__instance)) __result = true;
        }
    }
    [HarmonyLib.HarmonyPatch(typeof(EvolveItem), nameof(EvolveItem.ObjectInIndoorTrigger))]
    internal static class RackTriggerPatch
    {
        private static void Postfix(EvolveItem __instance, ref bool __result)
        {
            if (Main.Instance != null && Main.Instance.IsOnCuringRack(__instance)) __result = true;
        }
    }
}





