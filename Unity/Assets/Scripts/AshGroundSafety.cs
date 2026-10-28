using UnityEngine;

/// <summary>
/// Collision and ground safety for the runtime-generated Ash valley.
/// Keeps the character grounded even if a thin mesh-collider contact is missed,
/// and reinforces tree crowns and jagged rocks with primitive colliders.
/// </summary>
public static class AshGroundSafety
{
    const float WorldSize = 280f;
    const float TerrainOffsetZ = 45f;
    const float TerrainOffsetY = -0.15f;

    public static void PrepareWorld(Transform player)
    {
        GameObject world = GameObject.Find("Ash Valley World");
        if (world == null) return;

        Transform root = world.transform;
        if (root.Find("Ground safety floor") == null)
        {
            var floor = new GameObject("Ground safety floor");
            floor.transform.SetParent(root, false);
            floor.transform.position = new Vector3(0f, -20f, TerrainOffsetZ);
            floor.transform.localScale = new Vector3(WorldSize, 2f, WorldSize);
            var box = floor.AddComponent<BoxCollider>();
            box.isTrigger = false;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item.name == "Layered pine boughs")
            {
                var sphere = item.GetComponent<SphereCollider>();
                if (sphere == null) sphere = item.gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.43f;
                sphere.isTrigger = false;
            }
            else if (item.name == "Jagged mountain cliff" || item.name == "Weathered boulder" ||
                     item.name == "Layered valley cliff" || item.name == "Cliff ledge" ||
                     item.name == "Distant jagged mountain" || item.name == "Mountain broken crown")
            {
                var box = item.GetComponent<BoxCollider>();
                if (box == null) box = item.gameObject.AddComponent<BoxCollider>();
                box.center = Vector3.zero;
                box.size = Vector3.one * 0.88f;
                box.isTrigger = false;
            }
        }

        // Make sure every existing scenery collider is a solid collider.
        foreach (Collider collider in colliders)
            if (collider != null) collider.isTrigger = false;
    }

    /// <summary>
    /// Final grounding pass against the actual terrain mesh collider, not only the
    /// procedural height formula. This corrects pivot offsets and any small mismatch
    /// between generated object placement and the rendered landscape.
    /// </summary>
    public static void AlignSceneryToTerrain(Transform root)
    {
        if (root == null) return;
        Transform terrain = root.Find("Open World - sculpted ash valley");
        if (terrain == null) return;
        MeshCollider terrainCollider = terrain.GetComponent<MeshCollider>();
        if (terrainCollider == null || terrainCollider.sharedMesh == null) return;

        int aligned = 0;
        int skipped = 0;
        foreach (Transform item in root)
        {
            if (item == terrain || item.name == "Winding river - cold reflective water" ||
                item.name == "Ground safety floor")
                continue;

            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { skipped++; continue; }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 rayOrigin = new Vector3(item.position.x, 500f, item.position.z);
            RaycastHit hit;
            if (!terrainCollider.Raycast(new Ray(rayOrigin, Vector3.down), out hit, 1000f))
            {
                skipped++;
                continue;
            }

            // Set the rendered bottom on the mesh surface with a tiny anti-z-fighting lift.
            float delta = hit.point.y + 0.015f - bounds.min.y;
            if (Mathf.Abs(delta) > 0.001f)
            {
                item.position += Vector3.up * delta;
                aligned++;
            }
        }

        Debug.Log("[Ash Chronicles] Final terrain grounding pass: aligned=" + aligned +
            ", skipped=" + skipped + ", terrainCollider=" + terrainCollider.enabled);
    }

    public static float SampleGroundHeight(Vector3 worldPosition)
    {
        float x = Mathf.Clamp(worldPosition.x, -139.5f, 139.5f);
        float z = Mathf.Clamp(worldPosition.z - TerrainOffsetZ, -139.5f, 139.5f);

        // Keep this height function identical to AshOpenWorld.BuildTerrain.
        // Otherwise the player is snapped to an imaginary surface above/below the mesh.
        float macro = Mathf.Sin(x * .021f + Mathf.Sin(z * .013f) * .8f) * 5.2f
                    + Mathf.Cos(z * .024f + x * .009f) * 4.4f;
        float rolling = (Mathf.PerlinNoise((x + 450f) * .018f, (z + 450f) * .018f) - .5f) * 9.5f;
        float erosion = (Mathf.PerlinNoise((x + 90f) * .062f, (z + 90f) * .062f) - .5f) * 2.4f
                      + (Mathf.PerlinNoise((x - 170f) * .15f, (z + 270f) * .15f) - .5f) * .75f;
        float n = Mathf.PerlinNoise((x + 350f) * .010f, (z + 350f) * .010f);
        float ridgeNoise = 1f - Mathf.Abs(n * 2f - 1f);
        float ridge = Mathf.Pow(Mathf.Clamp01((ridgeNoise - .48f) / .52f), 2.0f) * 24f;
        float valleyAxis = RiverX(z);
        float valleyDistance = Mathf.Abs(x - valleyAxis);
        float valleyShoulder = Mathf.Clamp01((valleyDistance - 10f) / 43f);
        float riverBank = Mathf.Clamp01((valleyDistance - 3.6f) / 8.5f);
        float upland = macro + rolling + erosion + ridge * valleyShoulder;
        float localY = Mathf.Lerp(-.08f, upland, riverBank);
        localY -= Mathf.Exp(-Mathf.Pow(valleyDistance / 31f, 2f)) * 2.2f;
        return localY + TerrainOffsetY;
    }

    static float RiverX(float z)
    {
        return Mathf.Sin(z * 0.027f) * 15f + Mathf.Sin(z * 0.071f) * 4f;
    }
}