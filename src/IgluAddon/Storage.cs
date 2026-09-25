using System;
using System.Reflection;
using Il2Cpp;
using UnityEngine;
using MelonLoader;
using HarmonyLib;

namespace BurebistaFishingShelterFeatures
{
    public sealed partial class Main
    {
        private GameObject storageRoot;
        private Container storage;
        private Panel_Container storagePanel;
        private bool storageLoaded, storageFault;
        private object storageData;
        private MethodInfo storageLoad, storageSave;
        private static readonly string[] StorageMaterials = { "GEAR_ReclaimedWoodB", "GEAR_ScrapMetal", "GEAR_Cloth", "GEAR_GutDried" };
        private static readonly int[] StorageCounts = { 12, 6, 4, 4 };

        private bool StorageDataReady()
        {
            if (storageData == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = assembly.GetType("ModData.ModDataManager", false);
                    if (type == null) continue;
                    storageData = Activator.CreateInstance(type, "BurebistaFishingShelterStorage", false);
                    storageLoad = type.GetMethod("Load", new[] { typeof(string) });
                    storageSave = type.GetMethod("Save", new[] { typeof(string), typeof(string) });
                    break;
                }
            }
            return storageData != null && (bool)storageSave.Invoke(storageData, new object[] { "1", "ready" });
        }
        private string StorageKey => "region_" + activeScene;
        private void RestoreStorage()
        {
            if (storageLoaded || storageFault || root == null) return;
            try
            {
                if (!StorageDataReady()) return;
                StorageRecord record = StorageRecord.Decode((string)storageLoad.Invoke(storageData, new object[] { StorageKey }));
                if (record.Built)
                {
                    CreateStorage();
                    if (!string.IsNullOrEmpty(record.Contents))
                        storage.Deserialize(record.Contents, new Il2CppSystem.Collections.Generic.List<GearItem>());
                    ConfigureStorage();
                    storageRoot.SetActive(true);
                }
                storageLoaded = true;
            }
            catch (Exception error)
            {
                storageFault = true;
                if (storageRoot != null) storageRoot.SetActive(false);
                MelonLogger.Error("Almacen bloqueado para preservar su contenido: " + error);
                HUDMessage.AddMessage("No se pudo restaurar el almacen; sus datos se conservan.");
            }
        }
        internal void CaptureStorage()
        {
            if (!storageLoaded || storageFault || storage == null || activeSave != SaveGameSystem.GetCurrentSaveName()) return;
            try
            {
                var record = new StorageRecord { Built = true, Contents = SerializeStorage() };
                if (!(bool)storageSave.Invoke(storageData, new object[] { record.Encode(), StorageKey }))
                    throw new InvalidOperationException("ModData no esta listo");
            }
            catch (Exception error) { MelonLogger.Error("No se pudo guardar el almacen: " + error); HUDMessage.AddMessage("Error al guardar el almacen. Revisa Latest.log."); }
        }
        private string SerializeStorage()
        {
            bool disabled = storage.m_DisableSerialization;
            try
            {
                storage.m_DisableSerialization = false;
                string value = storage.Serialize();
                if (string.IsNullOrEmpty(value)) throw new InvalidOperationException("El contenedor no devolvio datos");
                return value;
            }
            finally { storage.m_DisableSerialization = disabled; }
        }
        private void ConfigureStorage()
        {
            storage.m_Capacity = Il2CppTLD.IntBackedUnit.ItemWeight.FromKilograms(500f);
            storage.m_StartInspected = true;
            storage.m_Inspected = true;
            storage.m_NotPopulated = false;
            storage.m_RolledSpawnChance = true;
            storage.m_SpawnChance = 100f;
            storage.m_MinRandomItems = storage.m_MaxRandomItems = 0;
            storage.m_FilterLocked = false;
            storage.m_OnlyShowSpecifiedGearType = false;
            storage.m_AllowDropAll = true;
            storage.m_DecayScalar = 1f;
            // ModData owns persistence. Avoid a second native container snapshot
            // restoring the same contents (or sending them to Lost and Found).
            storage.m_DisableSerialization = true;
        }
        private void HandleStorage()
        {
            if (!InsideShelter()) return;
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!Input.GetKeyDown(KeyCode.U) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
                || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return;
            RestoreStorage();
            if (!storageLoaded || storageFault)
            { HUDMessage.AddMessage("El almacen necesita ModData.dll cargado y sus datos disponibles."); return; }
            if (ctrl) { BuildStorage(); return; }
            if (storage == null) { HUDMessage.AddMessage("Ctrl+U: construir almacen de 500 kg."); return; }
            Transform player = GameManager.GetPlayerTransform();
            if (player == null || Vector3.Distance(player.position, storageRoot.transform.position) > 3f)
            { HUDMessage.AddMessage("Acercate al almacen para abrirlo con U."); return; }
            foreach (Panel_Container panel in Resources.FindObjectsOfTypeAll<Panel_Container>())
                if (panel != null && panel.gameObject.scene.IsValid())
                {
                    storagePanel = panel;
                    if (storage.m_ContainerPanel == null) storage.m_ContainerPanel = new Il2CppTLD.UI.Generics.PanelReference<Panel_Container>();
                    storage.m_ContainerPanel.InitializeReference();
                    panel.SetContainer(storage, "Almacen del iglu - 500 kg");
                    panel.Enable(true);
                    return;
                }
        }
        internal bool CloseStoragePanel(Container target)
        {
            if (storage == null || target != storage) return false;
            // This shelf has no lid/animation. Keep Panel_Container.OnDone's
            // normal navigation, bypass only this container's animated close.
            Panel_Container panel = storagePanel;
            if (panel == null && storage.m_ContainerPanel != null)
                storage.m_ContainerPanel.TryGetPanel(out panel);
            if (panel == null) return false;
            storage.m_PendingClose = false;
            storage.m_OpenInProgress = false;
            storage.m_IsAnimating = false;
            panel.Enable(false);
            storagePanel = null;
            CaptureStorage();
            return true;
        }
        private void BuildStorage()
        {
            if (storage != null) { HUDMessage.AddMessage("El almacen ya esta construido. U: abrir."); return; }
            Inventory inv = GameManager.GetInventoryComponent();
            if (inv == null) return;
            int[] before = new int[StorageCounts.Length];
            for (int i = 0; i < before.Length; i++)
            {
                before[i] = inv.GetNumGearWithName(StorageMaterials[i]);
                if (before[i] < StorageCounts[i])
                { HUDMessage.AddMessage("Almacen: 12 maderas recuperadas, 6 chatarras, 4 telas y 4 tripas curadas."); return; }
            }
            try
            {
                CreateStorage();
                for (int i = 0; i < before.Length; i++)
                {
                    inv.RemoveGearFromInventory(StorageMaterials[i], StorageCounts[i], true);
                    if (inv.GetNumGearWithName(StorageMaterials[i]) != before[i] - StorageCounts[i])
                        throw new InvalidOperationException("No se pudo confirmar el pago");
                }
                var record = new StorageRecord { Built = true, Contents = SerializeStorage() };
                if (!(bool)storageSave.Invoke(storageData, new object[] { record.Encode(), StorageKey }))
                    throw new InvalidOperationException("No se pudo registrar el almacen");
                storageRoot.SetActive(true);
                HUDMessage.AddMessage("Almacen de 500 kg construido. U: abrir. Guarda la partida para conservarlo.");
            }
            catch (Exception error)
            {
                if (storageRoot != null) UnityEngine.Object.Destroy(storageRoot);
                storageRoot = null; storage = null;
                for (int i = 0; i < before.Length; i++)
                {
                    int removed = Mathf.Clamp(before[i] - inv.GetNumGearWithName(StorageMaterials[i]), 0, StorageCounts[i]);
                    if (removed > 0) Refund(GameManager.GetPlayerManagerComponent(), StorageMaterials[i], removed);
                }
                MelonLogger.Error("Construccion del almacen cancelada: " + error);
                HUDMessage.AddMessage("Almacen cancelado; materiales devueltos.");
            }
        }
        private bool StorageBlocksChanges()
        {
            if (!storageLoaded && storageData != null)
            { HUDMessage.AddMessage("Espera a que se carguen los datos del almacen."); return true; }
            if (!storageFault && (storage == null || storage.IsEmpty())) return false;
            HUDMessage.AddMessage("Vacia el almacen antes de mover, redimensionar o desmontar el iglu.");
            return true;
        }
        private bool RemoveEmptyStorage()
        {
            if (storage == null) return true;
            try
            {
                if (!(bool)storageSave.Invoke(storageData, new object[] { new StorageRecord().Encode(), StorageKey })) return false;
                return true;
            }
            catch { return false; }
        }
    }
    [HarmonyPatch(typeof(Container), nameof(Container.BeginContainerClose))]
    internal static class StorageClosePatch
    {
        private static bool Prefix(Container __instance)
        {
            return Main.Instance == null || !Main.Instance.CloseStoragePanel(__instance);
        }
    }
    [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveSceneData))]
    internal static class StorageSavePatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix() { Main.Instance?.CaptureStorage(); }
    }
}
