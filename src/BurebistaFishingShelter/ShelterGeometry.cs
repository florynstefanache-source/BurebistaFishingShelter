using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BurebistaFishingShelter
{
    // Only called for our imported B3D meshes, never for game gear or furniture.
    internal static class ShelterGeometry
    {
        internal static Texture FindGameSnowTexture()
        {
            // Prefer the game's snow-shelter snow atlas, without borrowing its
            // fabric/rope atlas or a terrain shader with incompatible mesh inputs.
            foreach (Il2Cpp.SnowShelter shelter in Resources.FindObjectsOfTypeAll<Il2Cpp.SnowShelter>())
            {
                if (shelter == null) continue;
                foreach (Renderer renderer in shelter.GetComponentsInChildren<Renderer>(true))
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null || !material.HasProperty("_MainTex")) continue;
                        Texture texture = material.mainTexture;
                        if (texture == null) continue;
                        string name = texture.name.ToLowerInvariant();
                        if (name.Contains("snow") && !name.Contains("normal") && !name.Contains("mask")) return texture;
                    }
            }
            return null;
        }
        internal static void FinishRendering(Mesh mesh, MeshRenderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            renderer.receiveShadows = true;
            var inside = new GameObject("BurebistaInteriorSurface");
            inside.transform.SetParent(renderer.transform, false);
            Mesh reversed = UnityEngine.Object.Instantiate(mesh);
            reversed.name = "BurebistaInteriorMesh";
            for (int s = 0; s < reversed.subMeshCount; s++)
            {
                var indices = reversed.GetTriangles(s);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    int tmp = indices[i + 1]; indices[i + 1] = indices[i + 2]; indices[i + 2] = tmp;
                }
                reversed.SetTriangles(indices, s);
            }
            reversed.RecalculateNormals();
            inside.AddComponent<MeshFilter>().sharedMesh = reversed;
            var innerRenderer = inside.AddComponent<MeshRenderer>();
            var originals = renderer.sharedMaterials;
            var materials = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                materials[i] = new Material(originals[i]);
                materials[i].name = "BurebistaInteriorMaterial";
                materials[i].color = new Color(.38f, .40f, .42f, 1f);
            }
            innerRenderer.sharedMaterials = materials;
            innerRenderer.receiveShadows = true;
            innerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        internal static void AddSolidColliders(GameObject root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.sharedMesh.name != "B3DMesh") continue;
                if (filter.GetComponent<Collider>() != null) continue;
                var vertices = filter.sharedMesh.vertices;
                var triangles = filter.sharedMesh.triangles;
                var solidVertices = new List<Vector3>();
                var solidIndices = new List<int>();
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                    Vector3 cross = Vector3.Cross(b - a, c - a);
                    if (cross.sqrMagnitude < .000001f) continue;
                    Vector3 n = cross.normalized;
                    int start = solidVertices.Count;
                    Vector3 thickness = n * .35f;
                    solidVertices.Add(a + thickness); solidVertices.Add(b + thickness); solidVertices.Add(c + thickness);
                    solidVertices.Add(a - thickness); solidVertices.Add(b - thickness); solidVertices.Add(c - thickness);
                    // Closed triangular prism: both faces plus three edges.
                    int[] prism = {0,1,2, 5,4,3, 0,3,4, 0,4,1, 1,4,5, 1,5,2, 2,5,3, 2,3,0};
                    foreach (int index in prism) solidIndices.Add(start + index);
                    AddWallObstacle(filter.transform, a, b, c, n);
                }
                var mesh = new Mesh { name = "BurebistaSolidCollision" };
                mesh.vertices = solidVertices.ToArray(); mesh.triangles = solidIndices.ToArray(); mesh.RecalculateBounds();
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh; collider.convex = false; collider.isTrigger = false;
            }
        }

        internal static void AddClosedDoorBarrier(GameObject door)
        {
            // The rolled hide asset does not fill the aperture by itself.
            // This blocker exists only with the closed-door object.
            var barrier = new GameObject("BurebistaClosedDoorBarrier");
            barrier.transform.SetParent(door.transform, false);
            barrier.transform.localPosition = new Vector3(-.4f, 4f, 29.2f);
            var box = barrier.AddComponent<BoxCollider>();
            box.size = new Vector3(16.4f, 22f, 1f);
            box.isTrigger = false;
            var obstacle = barrier.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = box.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
        }

        private static void AddWallObstacle(Transform parent, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            // Roof projections would carve the entire interior out of the navmesh.
            // Only upright wall facets become navigation obstacles; all facets collide.
            if (Mathf.Abs(normal.y) > .35f) return;
            Vector3 forward = new Vector3(normal.x, 0, normal.z).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            Quaternion inverse = Quaternion.Inverse(rotation);
            Vector3 x = inverse * a, y = inverse * b, z = inverse * c;
            Vector3 min = Vector3.Min(x, Vector3.Min(y, z)), max = Vector3.Max(x, Vector3.Max(y, z));
            Vector3 size = max - min;
            if (size.x < 1f || size.y < 3f) return;
            var go = new GameObject("BurebistaWallNavigation");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = rotation * ((min + max) * .5f);
            go.transform.localRotation = rotation;
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(size.x, size.y, Mathf.Max(.7f, size.z));
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
        }

        internal static void ReleaseOwnedAssets(GameObject root)
        {
            if (root == null) return;
            // Meshes/materials/textures imported by this mod are runtime objects.
            // Gear resources are owned by the game and must never be destroyed here.
            var owned = new HashSet<UnityEngine.Object>();
            foreach (MeshFilter f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || (f.sharedMesh.name != "B3DMesh" && f.sharedMesh.name != "BurebistaInteriorMesh")) continue;
                owned.Add(f.sharedMesh);
                var renderer = f.GetComponent<Renderer>();
                if (renderer != null) foreach (Material m in renderer.sharedMaterials)
                {
                    if (m == null) continue;
                    owned.Add(m);
                    if (m.mainTexture != null && m.mainTexture.name.StartsWith("Burebista_", StringComparison.Ordinal)) owned.Add(m.mainTexture);
                }
                var collider = f.GetComponent<MeshCollider>();
                if (collider != null && collider.sharedMesh != null) owned.Add(collider.sharedMesh);
            }
            foreach (var asset in owned) UnityEngine.Object.Destroy(asset);
        }
    }
}





