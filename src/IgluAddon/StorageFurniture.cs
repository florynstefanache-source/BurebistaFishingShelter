using System;
using Il2Cpp;
using UnityEngine;

namespace BurebistaFishingShelterFeatures
{
    public sealed partial class Main
    {
        private void CreateStorage()
        {
            storageRoot = new GameObject("IntegratedStorage500kg");
            storageRoot.SetActive(false);
            storageRoot.transform.SetParent(root.transform.Find("BurebistaFurniture"), false);
            storageRoot.transform.localPosition = new Vector3(-17f, 0, -1f);
            storageRoot.transform.localRotation = Quaternion.Euler(0,90,0);
            UpdateStorageScale();
            Transform p = storageRoot.transform;
            Color wood = new Color(.32f,.21f,.12f,1);
            foreach (float x in new[] {-.86f,.86f}) foreach (float z in new[] {-.12f,.12f})
                Cube("StoragePost",p,new Vector3(x,.94f,z),new Vector3(.065f,1.88f,.065f),wood);
            foreach (float y in new[] {.12f,.65f,1.18f,1.72f})
                Cube("StorageShelf",p,new Vector3(0,y,0),new Vector3(1.8f,.065f,.30f),wood);
            foreach (float x in new[] {-.72f,-.36f,0,.36f,.72f})
                Cube("StorageBackPlank",p,new Vector3(x,.96f,.145f),new Vector3(.33f,1.76f,.025f),wood);
            // Render meshes only: decorations have no GearItem, pickup, decay,
            // light source or save component and never grant free equipment.
            StorageDecoration("GEAR_CannedBeans",new Vector3(-.65f,.685f,0),.24f);
            StorageDecoration("GEAR_CannedBeans",new Vector3(-.36f,.685f,0),.24f);
            StorageDecoration("GEAR_Peaches",new Vector3(.04f,.685f,0),.24f);
            StorageDecoration("GEAR_CondensedMilk",new Vector3(.43f,.685f,0),.24f);
            StorageDecoration("GEAR_Lantern",new Vector3(-.61f,1.215f,0),.38f);
            StorageDecoration("GEAR_Toolkit",new Vector3(0,1.215f,0),.45f);
            StorageDecoration("GEAR_Cloth",new Vector3(.57f,1.215f,0),.30f);
            StorageDecoration("GEAR_BedRoll",new Vector3(-.40f,.155f,0),.58f);
            StorageDecoration("GEAR_ReclaimedWoodB",new Vector3(.43f,.155f,0),.52f);
            storage = storageRoot.AddComponent<Container>();
            ConfigureStorage();
            // Activate before serialization so native Awake initializes the item lists.
            storageRoot.SetActive(true);
            ConfigureStorage();
        }
        private void UpdateStorageScale()
        {
            if (storageRoot == null) return;
            Vector3 scale = storageRoot.transform.parent.lossyScale;
            float fit = Mathf.Min(1f, Mathf.Abs(scale.x) / .08f);
            storageRoot.transform.localScale = new Vector3(fit/Mathf.Max(.001f,Mathf.Abs(scale.x)),
                fit/Mathf.Max(.001f,Mathf.Abs(scale.y)),fit/Mathf.Max(.001f,Mathf.Abs(scale.z)));
        }
        private void StorageDecoration(string gearName, Vector3 position, float maxSize)
        {
            GameObject display = null;
            try
            {
                GearItem prefab = GearItem.LoadGearItemPrefab(gearName);
                if (prefab == null) { MelonLoader.MelonLogger.Warning("Adorno no disponible: " + gearName); return; }
                display = new GameObject("Display_" + gearName);
                display.transform.SetParent(storageRoot.transform,false);
                foreach (MeshFilter source in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    Renderer original = source.GetComponent<Renderer>();
                    if (source.sharedMesh == null || original == null) continue;
                    var mesh = new GameObject("DisplayMesh");
                    mesh.transform.SetParent(display.transform,false);
                    mesh.transform.localPosition = prefab.transform.InverseTransformPoint(source.transform.position);
                    mesh.transform.localRotation = Quaternion.Inverse(prefab.transform.rotation) * source.transform.rotation;
                    Vector3 ss=source.transform.lossyScale, ps=prefab.transform.lossyScale;
                    mesh.transform.localScale=new Vector3(ss.x/ps.x,ss.y/ps.y,ss.z/ps.z);
                    mesh.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
                    MeshRenderer renderer=mesh.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials=original.sharedMaterials;
                    renderer.receiveShadows=true;
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                }
                // Bounds from mesh corners in display-local space work even while inactive.
                Bounds bounds=new Bounds(); bool first=true;
                foreach(MeshFilter mesh in display.GetComponentsInChildren<MeshFilter>(true))
                {
                    Bounds b=mesh.sharedMesh.bounds;
                    for(int i=0;i<8;i++)
                    {
                        Vector3 corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                        Vector3 v=display.transform.InverseTransformPoint(mesh.transform.TransformPoint(corner));
                        if(first){bounds=new Bounds(v,Vector3.zero);first=false;}else bounds.Encapsulate(v);
                    }
                }
                if(first){UnityEngine.Object.Destroy(display);return;}
                float factor=maxSize/Mathf.Max(.001f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
                display.transform.localScale=Vector3.one*factor;
                display.transform.localPosition=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*factor;
            }
            catch(Exception e)
            {
                if(display!=null)UnityEngine.Object.Destroy(display);
                MelonLoader.MelonLogger.Warning("Adorno omitido " + gearName + ": " + e.Message);
            }
        }
    }
}
