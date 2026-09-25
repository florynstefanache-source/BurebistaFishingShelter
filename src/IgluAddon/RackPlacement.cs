using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BurebistaFishingShelterFeatures
{
    public sealed partial class Main
    {
        private static readonly Vector3[] DryingSlots = {
            new Vector3(-.57f,1.02f,0), new Vector3(-.19f,1.02f,0),
            new Vector3(.19f,1.02f,0), new Vector3(.57f,1.02f,0),
            new Vector3(0,.56f,-.16f), new Vector3(0,.56f,.16f)
        };

        private static bool WorldRackItem(GearItem item)
        {
            return item != null && item.enabled && !item.NonInteractive && !item.m_InPlayerInventory
                && item.gameObject.activeInHierarchy && item.gameObject.scene.IsValid()
                && !item.IsAttachedToPlacePoint();
        }

        private void PlaceNearbyOnRack()
        {
            Transform player = GameManager.GetPlayerTransform();
            if (!hasRack || curingRack == null || player == null) return;
            Vector3 centre = curingRack.transform.TransformPoint(new Vector3(0,.6f,0));
            if (Vector3.Distance(player.position, centre) > 3f)
            {
                HUDMessage.AddMessage("Acercate al bastidor para colocar materiales con G.");
                return;
            }

            // Scan only on a key press. Native world items retain their identity,
            // scale, curing progress and normal game serialization; never clone or parent them.
            var items = new List<GearItem>();
            foreach (GearItem item in Resources.FindObjectsOfTypeAll<GearItem>())
                if (WorldRackItem(item))
                    items.Add(item);
            bool[] occupied = new bool[DryingSlots.Length];
            var alreadyPlaced = new HashSet<int>();
            foreach (GearItem item in items)
            {
                Vector3 local = curingRack.transform.InverseTransformPoint(item.transform.position);
                for (int i = 0; i < DryingSlots.Length; i++)
                    if (Vector3.Distance(local, DryingSlots[i]) < .075f)
                    {
                        occupied[i] = true;
                        alreadyPlaced.Add(item.GetInstanceID());
                    }
            }
            items.Sort((a,b) => (a.transform.position-centre).sqrMagnitude.CompareTo((b.transform.position-centre).sqrMagnitude));
            int placed = 0;
            foreach (GearItem item in items)
            {
                if (alreadyPlaced.Contains(item.GetInstanceID()) || !item.m_BeenInPlayerInventory
                    || Vector3.Distance(item.transform.position, centre) > 2.5f) continue;
                EvolveItem evolve = item.m_EvolveItem;
                if (evolve == null || evolve.m_TimeToEvolveGameDays <= 0 || item.IsWornOut()) continue;
                int kind = RackPlacementRules.Kind(item.name);
                int slot = RackPlacementRules.FreeSlot(kind, occupied);
                if (slot < 0) continue;
                Vector3 oldPosition = item.transform.position;
                Quaternion oldRotation = item.transform.rotation;
                Rigidbody body = item.m_RigidBody;
                bool oldKinematic = body != null && body.isKinematic;
                try
                {
                    // Native placed gear is stationary and remains directly pickable.
                    // No per-frame pose enforcement: pickup and manual placement stay native.
                    if (body != null) { if (!body.isKinematic) { body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero; } body.isKinematic = true; }
                    item.transform.position = curingRack.transform.TransformPoint(DryingSlots[slot]);
                    item.transform.rotation = curingRack.transform.rotation *
                        (kind == 2 ? Quaternion.Euler(0,90,0) : Quaternion.Euler(90,0,0));
                    occupied[slot] = true;
                    placed++;
                }
                catch (Exception error)
                {
                    item.transform.position = oldPosition;
                    item.transform.rotation = oldRotation;
                    if (body != null) body.isKinematic = oldKinematic;
                    MelonLoader.MelonLogger.Warning("No se pudo colocar un material: " + error.Message);
                }
            }
            HUDMessage.AddMessage(placed > 0 ? "Materiales colocados para secar: " + placed + ". Recogelos seleccionandolos."
                : "Sin sitio o materiales frescos cerca. Deja pieles, tripas o retonos junto al bastidor.");
        }
    }
}
