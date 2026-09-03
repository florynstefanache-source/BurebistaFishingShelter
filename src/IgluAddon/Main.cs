using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2Cpp;

[assembly: MelonInfo(typeof(BurebistaFishingShelterFeatures.Main), "Iglu Addon", "1.13.0", "Burebista")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace BurebistaFishingShelterFeatures
{
    public sealed class Main : MelonMod
    {
        internal static Main Instance;
        internal static bool Restoring;
        internal static bool Replacing;
        private GameObject root;
        private Bed bed;
        private Container storage;
        private GameObject fishRack;
        private int lastFishCount = -1;
        private float nextScan;
        private float nextSave;
        private GUIStyle style;
        private static readonly string StatePath = Path.Combine(AppContext.BaseDirectory, "Mods", "BurebistaFishingShelter", "shelter-v1.state");

        public override void OnInitializeMelon()
        {
            Instance = this;
            LoggerInstance.Msg("v1.13: bedroll rest input is checked every frame.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            root = null; bed = null; storage = null; fishRack = null; lastFishCount = -1;
            MelonCoroutines.Start(RestoreAfterSceneLoad(sceneName));
        }

        private System.Collections.IEnumerator RestoreAfterSceneLoad(string scene)
        {
            for (int i = 0; i < 80; i++) yield return null;
            if (!File.Exists(StatePath)) yield break;
            string[] p = File.ReadAllText(StatePath).Split('|');
            if (p.Length != 11 || p[0] != SaveGameSystem.GetCurrentSaveName() || p[1] != scene) yield break;
            Type t = AccessTools.TypeByName("BurebistaFishingShelter.Main");
            MethodInfo place = AccessTools.Method(t, "PlaceShelter");
            if (place == null) yield break;
            Restoring = true;
            try
            {
                place.Invoke(null, null);
                GameObject r = GameObject.Find("BurebistaFishingShelter");
                if (r != null)
                {
                    r.transform.position = new Vector3(F(p[2]), F(p[3]), F(p[4]));
                    r.transform.rotation = new Quaternion(F(p[5]), F(p[6]), F(p[7]), F(p[8]));
                    SetBaseScale(F(p[9]));
                    Attach(r);
                    if (storage != null && p[10].Length > 0)
                    {
                        string data = Encoding.UTF8.GetString(Convert.FromBase64String(p[10]));
                        storage.Deserialize(data, new Il2CppSystem.Collections.Generic.List<GearItem>());
                    }
                }
            }
            finally { Restoring = false; }
        }

        public override void OnUpdate()
        {
            // KeyDown must be checked every frame or short presses are missed.
            Transform player = GameManager.GetPlayerTransform();
            if (bed != null && player != null && Vector3.Distance(player.position, bed.transform.position) < 2.5f && Input.GetKeyDown(KeyCode.B))
                bed.PerformInteraction();

            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.25f;
            GameObject found = GameObject.Find("BurebistaFishingShelter");
            if (found != null && found != root) Attach(found);
            if (root == null) return;
            if(Time.unscaledTime >= nextSave){nextSave=Time.unscaledTime+2f;SaveState();}
        }

        public override void OnGUI()
        {
            // Never draw the construction interface over logos, loading screens or menus.
            if (GameManager.IsMainMenuActive() || GameManager.IsEmptySceneActive() || !GameManager.HasPlayerObject()) return;
            if (style == null) { style = new GUIStyle(GUI.skin.label); style.fontSize = 16; style.alignment = TextAnchor.MiddleCenter; style.normal.textColor = Color.white; }
            if(root==null)
            {
                Inventory inv=GameManager.GetInventoryComponent(); if(inv==null)return;
                string req="F8  CONSTRUIR IGLU   Palos "+inv.GetNumGearWithName("GEAR_Stick")+"/20   Telas "+inv.GetNumGearWithName("GEAR_Cloth")+"/5   Pieles curadas "+inv.GetNumGearWithName("GEAR_LeatherHideDried")+"/2";
                GUI.Label(new Rect(Screen.width/2f-430f,Screen.height-110f,860f,35f),req,style); return;
            }
            if (bed != null && GameManager.GetPlayerTransform() != null && Vector3.Distance(GameManager.GetPlayerTransform().position, bed.transform.position) < 2.5f)
                GUI.Label(new Rect(Screen.width / 2f - 180f, Screen.height - 220f, 360f, 30f), "B  DESCANSAR EN EL SACO", style);
        }

        internal void Attach(GameObject shelter)
        {
            root = shelter;
            if (shelter.transform.Find("BurebistaFurniture") != null) return;
            GameObject furniture = new GameObject("BurebistaFurniture");
            furniture.transform.SetParent(shelter.transform, false);

            AddHideDecorations(furniture.transform);
            AddIntegratedBedRoll(furniture.transform);
            SaveState();
        }

        private void AddIntegratedBedRoll(Transform parent)
        {
            GearItem prefab = GearItem.LoadGearItemPrefab("GEAR_BearSkinBedRoll");
            if (prefab == null) { MelonLogger.Warning("No se encontro GEAR_BearSkinBedRoll."); return; }
            GameObject g = UnityEngine.Object.Instantiate(prefab.gameObject, parent);
            g.name = "IntegratedBedRoll";
            g.transform.localPosition = new Vector3(0f, .18f, -11.5f);
            g.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            // The bearskin placed mesh is authored much larger than the regular
            // bedroll mesh. A 3.4 local scale matches the normal in-game bedroll.
            g.transform.localScale = Vector3.one * 3.4f;
            bed = g.GetComponent<Bed>();
            if (bed == null) bed = g.AddComponent<Bed>();
            bed.SetState(BedRollState.Placed);
            GearItem gear = g.GetComponent<GearItem>();
            if (gear != null) { gear.NonInteractive = true; gear.SetInteractive(false, false); gear.ToggleColliders(false); gear.enabled = false; }
            foreach (Collider c in g.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            bed.m_WarmthBonusCelsius = 12f;
            bed.m_ConditionPercentGainPerHour = 3f;
            bed.m_UinterruptedRestPercentGainPerHour = 12f;
        }

        private static void AddHuntingHooks(Transform parent)
        {
            CreateHuntingHook(parent, "RabbitHook", new Vector3(-8f,18f,1f), "GEAR_RabbitCarcass", 5f);
            CreateHuntingHook(parent, "PtarmiganHook", new Vector3(8f,18f,1f), "GEAR_PtarmiganCarcass", 4f);
        }

        private static void CreateHuntingHook(Transform parent, string name, Vector3 pos, string gearName, float animalScale)
        {
            GameObject hook=new GameObject(name); hook.transform.SetParent(parent,false); hook.transform.localPosition=pos;
            GameObject chain=GameObject.CreatePrimitive(PrimitiveType.Cylinder); chain.name="CeilingHook"; chain.transform.SetParent(hook.transform,false); chain.transform.localPosition=new Vector3(0f,-2.5f,0f); chain.transform.localScale=new Vector3(.22f,2.5f,.22f); chain.GetComponent<Renderer>().material.color=new Color(.12f,.12f,.11f,1f); UnityEngine.Object.Destroy(chain.GetComponent<Collider>());
            GameObject bar=GameObject.CreatePrimitive(PrimitiveType.Cylinder); bar.name="HangingBar"; bar.transform.SetParent(hook.transform,false); bar.transform.localPosition=new Vector3(0f,-5f,0f); bar.transform.localEulerAngles=new Vector3(0f,0f,90f); bar.transform.localScale=new Vector3(.25f,3.2f,.25f); bar.GetComponent<Renderer>().material.color=new Color(.12f,.12f,.11f,1f); UnityEngine.Object.Destroy(bar.GetComponent<Collider>());
            for(int i=0;i<3;i++) DecorativeCarcass(name+"Animal"+(i+1),gearName,hook.transform,new Vector3(-4f+i*4f,-8f,0f),new Vector3(0f,0f,180f),Vector3.one*animalScale);
        }

        private static void DecorativeCarcass(string name,string gearName,Transform parent,Vector3 pos,Vector3 rot,Vector3 scale)
        {
            GearItem prefab=GearItem.LoadGearItemPrefab(gearName); if(prefab==null){MelonLogger.Warning("No se encontro decoracion "+gearName);return;}
            GameObject g=UnityEngine.Object.Instantiate(prefab.gameObject,parent); g.name=name; g.transform.localPosition=pos; g.transform.localEulerAngles=rot; g.transform.localScale=scale;
            GearItem gear=g.GetComponent<GearItem>(); if(gear!=null){gear.NonInteractive=true;gear.SetInteractive(false,false);gear.ToggleColliders(false);gear.enabled=false;}
            foreach(Collider c in g.GetComponentsInChildren<Collider>(true))c.enabled=false;
        }

        private static void AddHideDecorations(Transform parent)
        {
            // Every hide has an exact mirrored partner. The bare centre is laid
            // out for a central campfire and one fishing hole on either side.
            DecorativeHide("BearBackLeft", "GEAR_BearHideDried", parent, new Vector3(-9.5f,.22f,-11.5f), new Vector3(0f,20f,0f), new Vector3(4.4f,4.4f,4.4f));
            DecorativeHide("BearBackRight", "GEAR_BearHideDried", parent, new Vector3(9.5f,.22f,-11.5f), new Vector3(0f,-20f,0f), new Vector3(4.4f,4.4f,4.4f));

            DecorativeHide("WolfSideLeft", "GEAR_WolfPeltDried", parent, new Vector3(-13.5f,.23f,-2f), new Vector3(0f,75f,0f), new Vector3(4.5f,4.5f,4.5f));
            DecorativeHide("WolfSideRight", "GEAR_WolfPeltDried", parent, new Vector3(13.5f,.23f,-2f), new Vector3(0f,-75f,0f), new Vector3(4.5f,4.5f,4.5f));

            DecorativeHide("DeerSideLeft", "GEAR_LeatherHideDried", parent, new Vector3(-13.5f,.24f,7f), new Vector3(0f,100f,0f), new Vector3(4.8f,4.8f,4.8f));
            DecorativeHide("DeerSideRight", "GEAR_LeatherHideDried", parent, new Vector3(13.5f,.24f,7f), new Vector3(0f,-100f,0f), new Vector3(4.8f,4.8f,4.8f));

            DecorativeHide("RabbitFrontOuterLeft", "GEAR_RabbitPeltDried", parent, new Vector3(-9f,.26f,13.5f), new Vector3(0f,20f,0f), new Vector3(4.4f,4.4f,4.4f));
            DecorativeHide("RabbitFrontOuterRight", "GEAR_RabbitPeltDried", parent, new Vector3(9f,.26f,13.5f), new Vector3(0f,-20f,0f), new Vector3(4.4f,4.4f,4.4f));
            DecorativeHide("RabbitFrontInnerLeft", "GEAR_RabbitPeltDried", parent, new Vector3(-3.5f,.27f,15f), new Vector3(0f,-10f,0f), new Vector3(4.2f,4.2f,4.2f));
            DecorativeHide("RabbitFrontInnerRight", "GEAR_RabbitPeltDried", parent, new Vector3(3.5f,.27f,15f), new Vector3(0f,10f,0f), new Vector3(4.2f,4.2f,4.2f));
        }

        private static void DecorativeHide(string name, string gearName, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            GearItem prefab = GearItem.LoadGearItemPrefab(gearName);
            if(prefab==null)return;
            GameObject g=UnityEngine.Object.Instantiate(prefab.gameObject,parent); g.name=name; g.transform.localPosition=pos; g.transform.localEulerAngles=rot; g.transform.localScale=scale;
            GearItem gear=g.GetComponent<GearItem>(); if(gear!=null){gear.NonInteractive=true;gear.SetInteractive(false,false);gear.ToggleColliders(false);gear.enabled=false;}
            foreach(Collider c in g.GetComponentsInChildren<Collider>(true)) c.enabled=false;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale;
            g.GetComponent<Renderer>().material.color=color; return g;
        }

        private static void CreateCookingMarkers(Transform parent)
        {
            for(int i=0;i<3;i++)
            {
                GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name="CookingPlace"+(i+1); g.transform.SetParent(parent,false);
                g.transform.localPosition=new Vector3(-8f+i*8f,.6f,12f); g.transform.localScale=new Vector3(2.8f,.5f,2.8f);
                g.GetComponent<Renderer>().material.color=new Color(.18f,.18f,.18f,1f);
            }
        }

        private void EnsureThreeCookingPlaces()
        {
            Fire[] fires=Resources.FindObjectsOfTypeAll<Fire>();
            foreach(Fire fire in fires)
            {
                if(fire==null || Vector3.Distance(fire.transform.position,root.transform.position)>3f) continue;
                CookingSlot[] slots=fire.GetComponentsInChildren<CookingSlot>(true);
                if(slots.Length==0 || slots.Length>=3) return;
                for(int i=slots.Length;i<3;i++)
                {
                    GameObject clone=UnityEngine.Object.Instantiate(slots[0].gameObject,slots[0].transform.parent);
                    clone.name="BurebistaCookingSlot"+(i+1);
                    clone.transform.localPosition=slots[0].transform.localPosition+new Vector3((i-slots.Length+1)*.45f,0f,0f);
                }
                return;
            }
        }

        private void UpdateFishRack()
        {
            if(storage==null || fishRack==null) return;
            List<string> fishNames=new List<string>();
            foreach(var item in storage.m_Items)
            {
                string n=(item.m_GearItemName??"").ToLowerInvariant();
                if(n.Contains("fish") || n.Contains("trout") || n.Contains("salmon") || n.Contains("bass")) fishNames.Add(item.m_GearItemName);
            }
            int count=Math.Min(fishNames.Count,6); if(count==lastFishCount)return; lastFishCount=count;
            for(int i=fishRack.transform.childCount-1;i>=1;i--) UnityEngine.Object.Destroy(fishRack.transform.GetChild(i).gameObject);
            for(int i=0;i<count;i++)
            {
                GearItem prefab=GearItem.LoadGearItemPrefab(fishNames[i]);
                GameObject f=prefab!=null?UnityEngine.Object.Instantiate(prefab.gameObject,fishRack.transform):GameObject.CreatePrimitive(PrimitiveType.Capsule);
                f.name="StoredFishVisual"; f.transform.SetParent(fishRack.transform,false); f.transform.localPosition=new Vector3(-7.5f+i*3f,-3f,0f); f.transform.localRotation=Quaternion.Euler(0f,0f,90f); f.transform.localScale=Vector3.one*5f;
                GearItem visualGear=f.GetComponent<GearItem>(); if(visualGear!=null){visualGear.NonInteractive=true;visualGear.SetInteractive(false,false);visualGear.ToggleColliders(false);visualGear.enabled=false;}
                foreach(Collider c in f.GetComponentsInChildren<Collider>(true)) c.enabled=false;
            }
        }

        internal bool BeforePlace()
        {
            if(Restoring) return true;
            if(GameObject.Find("BurebistaFishingShelter")!=null){Replacing=true;return true;}
            Inventory inv=GameManager.GetInventoryComponent();
            if(inv.GetNumGearWithName("GEAR_Stick")<20 || inv.GetNumGearWithName("GEAR_Cloth")<5 || inv.GetNumGearWithName("GEAR_LeatherHideDried")<2)
            { HUDMessage.AddMessage("Necesitas 20 palos, 5 telas y 2 pieles de ciervo curadas."); MelonLogger.Msg("Faltan materiales para construir el iglu."); return false; }
            inv.RemoveGearFromInventory("GEAR_Stick",20,true); inv.RemoveGearFromInventory("GEAR_Cloth",5,true); inv.RemoveGearFromInventory("GEAR_LeatherHideDried",2,true); return true;
        }
        internal void AfterPlace(){Replacing=false; GameObject r=GameObject.Find("BurebistaFishingShelter"); if(r!=null)Attach(r);}
        internal void BeforeRemove()
        {
            if(Restoring||Replacing)return; if(GameObject.Find("BurebistaFishingShelter")==null)return;
            PlayerManager pm=GameManager.GetPlayerManagerComponent(); Refund(pm,"GEAR_Stick",10); Refund(pm,"GEAR_Cloth",2); Refund(pm,"GEAR_LeatherHideDried",1);
            if(File.Exists(StatePath))File.Delete(StatePath);
        }
        private static void Refund(PlayerManager pm,string name,int count){GearItem prefab=GearItem.LoadGearItemPrefab(name); if(prefab!=null)pm.InstantiateItemInPlayerInventory(prefab,count,100f,PlayerManager.InventoryInstantiateFlags.None);}
        private void SaveState(){if(root==null)return;Directory.CreateDirectory(Path.GetDirectoryName(StatePath));Vector3 p=root.transform.position;Quaternion q=root.transform.rotation;string box=storage==null?"":Convert.ToBase64String(Encoding.UTF8.GetBytes(storage.Serialize()));File.WriteAllText(StatePath,string.Join("|",SaveGameSystem.GetCurrentSaveName(),UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,S(p.x),S(p.y),S(p.z),S(q.x),S(q.y),S(q.z),S(q.w),S(root.transform.localScale.x),box));}
        private static string S(float v)=>v.ToString("R",CultureInfo.InvariantCulture);
        private static float F(string s){float.TryParse(s,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float v);return v;}
        private static void SetBaseScale(float s){Type t=AccessTools.TypeByName("BurebistaFishingShelter.Main");AccessTools.Field(t,"shelterScale").SetValue(null,s);GameObject r=GameObject.Find("BurebistaFishingShelter");if(r!=null)r.transform.localScale=Vector3.one*s;}
    }

    [HarmonyPatch]
    internal static class PlacePatch
    { static MethodBase TargetMethod()=>AccessTools.Method(AccessTools.TypeByName("BurebistaFishingShelter.Main"),"PlaceShelter"); static bool Prefix()=>Main.Instance==null||Main.Instance.BeforePlace(); static void Postfix(){Main.Instance?.AfterPlace();} }
    [HarmonyPatch]
    internal static class RemovePatch
    { static MethodBase TargetMethod()=>AccessTools.Method(AccessTools.TypeByName("BurebistaFishingShelter.Main"),"RemoveShelter"); static void Prefix(){Main.Instance?.BeforeRemove();} }
}
