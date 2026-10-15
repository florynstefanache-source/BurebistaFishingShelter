using BurebistaFishingShelter;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2Cpp;

namespace BurebistaFishingShelterFeatures
{
    public sealed partial class Main : MelonMod
    {
        internal static Main Instance;
        internal static bool Restoring, Replacing;
        private GameObject root;
        private Bed bed;
        private GUIStyle style;
        private float nextScan, nextSave;
        private string activeSave, activeScene, legacyContainer = "";
        internal static readonly bool UsesNativeSnowRecipe = true;
        private object snowDebit;
        private GearItem snowDoor;
        private bool pendingConstruction;
        private bool canInput, showHints;
        private string lastSavedPayload;
        private readonly RestoreGate restoreGate = new RestoreGate();
        private string observedSave, observedScene;
        private static readonly string StateDirectory = Path.Combine(AppContext.BaseDirectory, "Mods", "BurebistaFishingShelter");
        // Targeted assembly lookup avoids Harmony's AllTypes() scan of every
        // loaded game/mod assembly. Resolve once, never from an IMGUI repaint.
        internal static readonly Type BaseType = ResolveBaseType();
        private static Type ResolveBaseType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetName().Name == "BurebistaFishingShelter")
                    return assembly.GetType("BurebistaFishingShelter.Main", true);
            throw new TypeLoadException("Falta BurebistaFishingShelter.dll; instala juntos los tres modulos.");
        }
        private static readonly System.Collections.Generic.Dictionary<string, FieldInfo> BaseFields = new System.Collections.Generic.Dictionary<string, FieldInfo>();
        private static FieldInfo FindBaseField(string name)
        {
            if (!BaseFields.TryGetValue(name, out FieldInfo field))
            {
                field = BaseType.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) throw new MissingFieldException(BaseType.FullName, name);
                BaseFields.Add(name, field);
            }
            return field;
        }
        private static object BaseField(string name) => FindBaseField(name).GetValue(null);
        private static void SetBaseField(string name, object value) => FindBaseField(name).SetValue(null, value);
        private static GameObject CurrentShelter() => BaseField("shelterRoot") as GameObject;
        private static string StatePath(string save, string scene)
        {
            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(save + "\0" + scene)));
            return Path.Combine(StateDirectory, "states-v2", key + ".json");
        }
        public override void OnInitializeMelon()
        {
            Instance = this;
            LoggerInstance.Msg("v1.16.0: configurable controls in Mod Settings; 40 whole snow blocks + 1 cured deer hide.");
        }
        // Do not reset on OnSceneWasLoaded: TLD loads GEAR/EXTRA/WILDLIFE additively.
        private void ClearReferences()
        {
            storagePanel = null; storage = null; storageRoot = null; storageLoaded = false; storageFault = false;
            root = null; bed = null; workbench = null; curingRack = null;
            hasWorkbench = false; hasRack = false; Restoring = false; Replacing = false;
            pendingConstruction = false; activeSave = null; activeScene = null; legacyContainer = "";
            lastSavedPayload = null;
            foreach (Material m in furnitureMaterials) if (m != null) UnityEngine.Object.Destroy(m);
            furnitureMaterials.Clear();
            if (fallbackWood != null) UnityEngine.Object.Destroy(fallbackWood);
            fallbackWood = null;
        }
        private void RestoreForSession(string save, string scene)
        {
            if (root != null) return;
            try
            {
                string path = StatePath(save, scene);
                if (!File.Exists(path)) path = Path.Combine(StateDirectory, "shelter-v1.state");
                if (!File.Exists(path)) return;
                ShelterState state = ShelterState.Decode(File.ReadAllText(path), save, scene);
                if (state == null || state.Removed) return;
                MelonLogger.Msg("Restaurando iglu guardado: " + save + " / " + scene + ", escala " + state.Scale);
                Restoring = true;
                SetBaseField("variant", state.Variant);
                SetBaseField("shelterScale", state.Scale);
                AccessTools.Method(BaseType, "PlaceShelter").Invoke(null, null);
                GameObject shelter = CurrentShelter();
                if (shelter == null) throw new InvalidOperationException("El cargador no creo el refugio; se conserva el estado original.");
                shelter.transform.position = new Vector3(state.Position[0], state.Position[1], state.Position[2]);
                shelter.transform.rotation = new Quaternion(state.Rotation[0], state.Rotation[1], state.Rotation[2], state.Rotation[3]).normalized;
                shelter.transform.localScale = Vector3.one * state.Scale;
                SetBaseField("closedDoor", state.ClosedDoor);
                AccessTools.Method(BaseType, "SpawnDoor").Invoke(null, null);
                hasWorkbench = state.Workbench; hasRack = state.Rack; legacyContainer = state.LegacyContainer;
                Attach(shelter);
                MelonLogger.Msg("Iglu restaurado en su posicion guardada. No se han consumido materiales.");
            }
            catch (Exception e) { MelonLogger.Error("No se pudo restaurar el refugio: " + e); }
            finally { Restoring = false; }
            if (root != null) SaveState();
        }
        public override void OnUpdate()
        {
            canInput = false;
            if (!TryGetSession(out string save, out string scene))
            {
                // Only an actual menu/empty active scene ends the session. A player
                // still being initialized must not permanently cancel restoration.
                string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? "";
                if (current == "Empty" || current.Contains("MainMenu", StringComparison.OrdinalIgnoreCase) || current.Contains("Boot", StringComparison.OrdinalIgnoreCase))
                {
                    restoreGate.Reset(); observedSave = null; observedScene = null;
                    if (root != null || activeSave != null) ClearReferences();
                }
                return;
            }
            if (observedSave != save || observedScene != scene)
            {
                ClearReferences(); observedSave = save; observedScene = scene;
            }
            bool inputReady = InputReady();
            canInput = inputReady;
            showHints = (bool)BaseField("ShowHints");
            if (restoreGate.Observe(save, scene, inputReady, Time.unscaledTime)) RestoreForSession(save, scene);
            if (inputReady)
            {
                Transform player = GameManager.GetPlayerTransform();
                if (bed != null && player != null && Vector3.Distance(player.position, bed.transform.position) < 2.5f && Input.GetKeyDown(KeyCode.B))
                    bed.PerformInteraction();
                HandleUpgrades();
                HandleStorage();
            }
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + .25f;
            UpdateFurnitureScale();
            RestoreStorage();
            if (root == null)
            {
                GameObject found = CurrentShelter();
                if (found != null && !Restoring) Attach(found);
            }
            if (root != null && Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 2f;
                SaveState();
            }
        }
        public override void OnGUI()
        {
            if (!showHints || !canInput || Event.current.type != EventType.Repaint) return;
            if (style == null) { style = new GUIStyle(GUI.skin.label); style.fontSize = 16; style.alignment = TextAnchor.MiddleCenter; style.normal.textColor = Color.white; }
            if (root == null)
            {
                Inventory inv = GameManager.GetInventoryComponent(); if (inv == null) return;
                string req = IgluSettings.Current.Build + IgluText.Pick(" CONSTRUIR IGLÚ   Bloques enteros "," BUILD IGLOO   Whole snow blocks ") + SnowConstruction.Count() + "/40   " + IgluText.Pick("Piel de ciervo curada ","Cured deer hide ") + inv.GetNumGearWithName("GEAR_LeatherHideDried") + "/1   | " + IgluSettings.Current.Hints + IgluText.Pick(" ocultar ayudas", " hide hints");
                GUI.Label(new Rect(10, Screen.height - 110, Screen.width - 20, 35), req, style);
                return;
            }
            if (bed != null && InsideShelter())
                GUI.Label(new Rect(10, Screen.height - 220, Screen.width - 20, 30), IgluText.Pick("B DESCANSAR EN EL SACO   | ", "B REST IN BEDROLL   | ") + IgluSettings.Current.Hints + IgluText.Pick(" ocultar ayudas", " hide hints"), style);
            DrawUpgrades();
            if (InsideShelter()) GUI.Label(new Rect(10, Screen.height - 345, Screen.width - 20, 30),
                storage != null ? IgluText.Pick("U  ABRIR ALMACEN - 500 kg", "U  OPEN STORAGE - 500 kg") : IgluText.Pick("Ctrl+U  ALMACEN: 12 maderas recuperadas, 6 chatarras, 4 telas, 4 tripas curadas", "Ctrl+U  STORAGE: 12 reclaimed wood, 6 scrap metal, 4 cloth, 4 cured guts"), style);
        }
        private static bool TryGetSession(out string save, out string scene)
        {
            save = null; scene = null;
            try
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(scene) || scene == "Empty" || scene.Contains("MainMenu", StringComparison.OrdinalIgnoreCase) || scene.Contains("Boot", StringComparison.OrdinalIgnoreCase)) return false;
                if (GameManager.IsMainMenuActive() || GameManager.IsEmptySceneActive() || !GameManager.HasPlayerObject() || GameManager.GetPlayerTransform() == null || GameManager.GetPlayerManagerComponent() == null) return false;
                save = SaveGameSystem.GetCurrentSaveName();
                return !string.IsNullOrEmpty(save);
            }
            catch { return false; } // Native singletons can be incomplete during loading.
        }
        private static bool InputReady()
        {
            // Called once from OnUpdate, after TryGetSession succeeded.
            try { return Time.timeScale > 0f && !GameManager.ControlsLocked(); }
            catch { return false; }
        }
        internal void Attach(GameObject shelter)
        {
            root = shelter;
            activeSave = SaveGameSystem.GetCurrentSaveName(); activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (shelter.transform.Find("BurebistaFurniture") != null) return;
            foreach (Material m in furnitureMaterials) if (m != null) UnityEngine.Object.Destroy(m);
            furnitureMaterials.Clear();
            var furniture = new GameObject("BurebistaFurniture");
            furniture.transform.SetParent(shelter.transform, false);
            AddHideDecorations(furniture.transform); AddIntegratedBedRoll(furniture.transform);
            if (hasWorkbench) CreateUpgrade(true).SetActive(true);
            if (hasRack) CreateUpgrade(false).SetActive(true);
            storageLoaded = false; storage = null; storageRoot = null;
            RestoreStorage();
            if (!Restoring) SaveState();
        }
        private bool RackOccupied()
        {
            if (!hasRack || curingRack == null) return false;
            foreach (GearItem item in Resources.FindObjectsOfTypeAll<GearItem>())
            {
                if (item == null || item.m_InPlayerInventory || !item.gameObject.activeInHierarchy || !item.gameObject.scene.IsValid()) continue;
                Vector3 p = curingRack.transform.InverseTransformPoint(item.transform.position);
                if (RackContains(p, true)) return true;
            }
            return false;
        }
        internal bool AllowScaleChange()
        {
            if (StorageBlocksChanges()) return false;
            if (!RackOccupied()) return true;
            HUDMessage.AddMessage(IgluText.Pick("Recoge los objetos del bastidor antes de cambiar el tamano.", "Collect the items from the curing rack before resizing."));
            return false;
        }
        internal bool BeforePlace()
        {
            RollbackSnow();
            pendingConstruction = false;
            if (Restoring) return true;
            if (!restoreGate.Attempted) { HUDMessage.AddMessage(IgluText.Pick("Espera a que termine la restauracion del iglu guardado.", "Wait for the saved igloo to finish restoring.")); return false; }
            if (StorageBlocksChanges()) return false;
            if (storage != null) CaptureStorage();
            if (RackOccupied()) { HUDMessage.AddMessage(IgluText.Pick("Recoge los objetos del bastidor antes de mover el iglu.", "Collect the items from the curing rack before moving the igloo.")); return false; }
            if (CurrentShelter() != null) { Replacing = true; return true; }
            Inventory inv = GameManager.GetInventoryComponent();
            try {
                snowDoor = inv?.GetHighestConditionGearThatMatchesName("GEAR_LeatherHideDried");
                if (snowDoor == null || snowDoor.GetNormalizedCondition() <= 0) throw new Exception(IgluText.Pick("Necesitas 40 bloques enteros de nieve y 1 piel curada de ciervo.","You need 40 whole snow blocks and 1 cured deer hide."));
                snowDebit = SnowConstruction.Reserve();
            } catch (Exception e) { RollbackSnow(); HUDMessage.AddMessage(e.InnerException?.Message ?? e.Message); return false; }
            pendingConstruction = true; return true;
        }
        internal void AfterPlace(bool ran)
        {
            if (!ran || Restoring) { RollbackSnow(); return; }
            GameObject shelter = CurrentShelter();
            if (shelter != null)
            {
                if (pendingConstruction)
                {
                    Inventory inv = GameManager.GetInventoryComponent();
                    SnowConstruction.Invoke(snowDebit, "Commit"); inv.RemoveUnits(snowDoor, 1); snowDebit = null; snowDoor = null;
                    hasWorkbench = false; hasRack = false;
                }
                Attach(shelter);
            }
            RollbackSnow(); pendingConstruction = false; Replacing = false;
        }
        internal void RollbackSnow() { SnowConstruction.Invoke(snowDebit, "Rollback"); snowDebit = null; snowDoor = null; }
        internal bool BeforeRemove()
        {
            if (Restoring || Replacing) return true;
            if (CurrentShelter() == null) return true;
            if (RackOccupied()) { HUDMessage.AddMessage(IgluText.Pick("Recoge los objetos del bastidor antes de desmontar el iglu.", "Collect the items from the curing rack before dismantling the igloo.")); return false; }
            if (StorageBlocksChanges()) return false;
            if (!SaveState(true)) { HUDMessage.AddMessage(IgluText.Pick("No se pudo guardar el desmontaje.", "Could not save the dismantling.")); return false; }
            if (!RemoveEmptyStorage()) { SaveState(false); HUDMessage.AddMessage(IgluText.Pick("No se pudo registrar el desmontaje del almacen.", "Could not save the storage dismantling.")); return false; }
            PlayerManager pm = GameManager.GetPlayerManagerComponent();
            Refund(pm, "GEAR_LeatherHideDried", 1);
            if (hasWorkbench) { Refund(pm, BenchMaterials[0], 3); Refund(pm, BenchMaterials[1], 2); Refund(pm, BenchMaterials[2], 1); }
            if (hasRack) { Refund(pm, RackMaterials[0], 6); Refund(pm, RackMaterials[1], 2); Refund(pm, RackMaterials[2], 1); }
            if (storage != null) for (int i=0;i<StorageCounts.Length;i++) Refund(pm, StorageMaterials[i], StorageCounts[i]/2);
            ClearReferences(); return true;
        }
        private static void Refund(PlayerManager pm, string name, int count)
        {
            GearItem prefab = GearItem.LoadGearItemPrefab(name);
            if (prefab != null && pm != null) pm.InstantiateItemInPlayerInventory(prefab, count, 100f, PlayerManager.InventoryInstantiateFlags.None);
        }
        private bool SaveState(bool removed = false)
        {
            if (root == null || Restoring || string.IsNullOrEmpty(activeSave) || string.IsNullOrEmpty(activeScene)) return false;
            try
            {
                Vector3 p = root.transform.position; Quaternion q = root.transform.rotation;
                var state = new ShelterState {
                    Save = activeSave, Scene = activeScene,
                    Position = new[] {p.x, p.y, p.z}, Rotation = new[] {q.x, q.y, q.z, q.w}, Scale = root.transform.localScale.x,
                    Variant = (int)BaseField("placedVariant"), ClosedDoor = (bool)BaseField("closedDoor"),
                    Workbench = hasWorkbench, Rack = hasRack, Removed = removed, LegacyContainer = legacyContainer
                };
                string path = StatePath(activeSave, activeScene);
                string payload = state.Encode();
                if (payload == lastSavedPayload) return true;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", payload);
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                File.Move(path + ".tmp", path, true);
                lastSavedPayload = payload;
                return true;
            }
            catch (Exception e) { MelonLogger.Error("No se pudo guardar el refugio: " + e.Message); return false; }
        }
    }
    [HarmonyPatch]
    internal static class PlacePatch
    {
        static MethodBase TargetMethod() => AccessTools.Method(Main.BaseType, "PlaceShelter");
        static bool Prefix() => Main.Instance == null || Main.Instance.BeforePlace();
        static void Postfix(bool __runOriginal) { Main.Instance?.AfterPlace(__runOriginal); }
        static Exception Finalizer(Exception __exception) { if (__exception != null) Main.Instance?.RollbackSnow(); return __exception; }
    }
    [HarmonyPatch]
    internal static class ScalePatch
    {
        static MethodBase TargetMethod() => AccessTools.Method(Main.BaseType, "AdjustScale");
        static bool Prefix() => Main.Instance == null || Main.Instance.AllowScaleChange();
        static void Postfix() => Main.Instance?.UpdateFurnitureScale();
    }
    [HarmonyPatch]
    internal static class RemovePatch
    {
        static MethodBase TargetMethod() => AccessTools.Method(Main.BaseType, "RemoveShelter");
        static bool Prefix() => Main.Instance == null || Main.Instance.BeforeRemove();
    }
}






