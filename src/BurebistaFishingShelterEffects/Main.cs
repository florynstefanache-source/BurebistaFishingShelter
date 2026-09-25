using MelonLoader;
using UnityEngine;
using Il2Cpp;

[assembly: MelonInfo(typeof(BurebistaFishingShelterEffects.Main), "Burebista Fishing Shelter Effects", "1.15.1", "Burebista")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace BurebistaFishingShelterEffects
{
    public sealed class Main : MelonMod
    {
        private GameObject attachedShelter;
        private float nextSearch;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("+15 C and indoor weather protection loaded.");
        }

        public override void OnUpdate()
        {
            if (attachedShelter != null || Time.unscaledTime < nextSearch) return;
            nextSearch = Time.unscaledTime + .5f;
            GameObject shelter = GameObject.Find("BurebistaFishingShelter");
            if (shelter == null)
                return;

            if (shelter != attachedShelter)
            {
                attachedShelter = shelter;
                AddInteriorVolume(shelter);
            }

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

            // Lit, inward-facing surfaces and solid collisions are built by the B3D loader.
            MelonLogger.Msg("[FishingShelter] +15 C, viento y nieve bloqueados; zona exterior utilizable.");
        }

    }
}






