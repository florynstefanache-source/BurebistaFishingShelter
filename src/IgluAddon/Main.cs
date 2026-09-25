using System;
using UnityEngine;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(BurebistaFishingShelterFeatures.Main), "Iglu Addon", "1.15.1", "Burebista")]
[assembly: MelonGame("Hinterland", "TheLongDark")]
namespace BurebistaFishingShelterFeatures
{
    public sealed partial class Main
    {
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

        private GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale;
            Material material = FurnitureMaterial(name, color);
            Renderer renderer = g.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.receiveShadows = true; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return g;
        }

    }
}





