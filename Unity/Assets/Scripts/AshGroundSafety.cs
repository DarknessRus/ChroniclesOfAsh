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
            floor.transform.position = new Vector3(0f, -8f, TerrainOffsetZ);
            floor.transform.localScale = new Vector3(WorldSize, 1f, WorldSize);
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
            else if (item.name == "Jagged mountain cliff" || item.name == "Weathered boulder")
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

    public static float SampleGroundHeight(Vector3 worldPosition)
    {
        float x = Mathf.Clamp(worldPosition.x, -139.5f, 139.5f);
        float z = Mathf.Clamp(worldPosition.z - TerrainOffsetZ, -139.5f, 139.5f);

        float broad = Mathf.Sin(x * 0.034f) * 2.4f + Mathf.Cos(z * 0.031f) * 2.2f;
        float detail = (Mathf.PerlinNoise((x + 450f) * 0.035f, (z + 450f) * 0.035f) - 0.5f) * 2.6f
                     + (Mathf.PerlinNoise((x + 90f) * 0.095f, (z + 90f) * 0.095f) - 0.5f) * 0.75f;
        float ridgeNoise = Mathf.PerlinNoise((x + 350f) * 0.012f, (z + 350f) * 0.012f);
        float ridge = Mathf.Pow(Mathf.Clamp01((ridgeNoise - 0.43f) / 0.57f), 1.4f) * 13f;
        float riverDistance = Mathf.Abs(x - RiverX(z));
        float bank = Mathf.Clamp01((riverDistance - 4.2f) / 10f);
        float localY = Mathf.Lerp(-0.08f, broad + detail + ridge, bank);
        return localY + TerrainOffsetY;
    }

    static float RiverX(float z)
    {
        return Mathf.Sin(z * 0.027f) * 15f + Mathf.Sin(z * 0.071f) * 4f;
    }
}