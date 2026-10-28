using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds imported CC0 environment meshes on top of the existing procedural valley.
/// Every instance is grounded using its actual rendered bounds, so the asset pivot
/// cannot leave a tree, rock or fallen trunk hovering above the terrain.
/// </summary>
public static class AshEnvironmentModelScatter
{
    static GameObject Load(string resourcePath)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
            Debug.LogWarning("[Ash Chronicles] Environment model missing: " + resourcePath);
        return prefab;
    }

    static GameObject Place(GameObject prefab, Transform root, string label,
        Vector3 position, float scale, float yaw)
    {
        if (prefab == null) return null;
        GameObject instance = Object.Instantiate(prefab, root, false);
        instance.name = label;
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = Vector3.one * scale;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float correction = position.y - bounds.min.y;
            instance.transform.position += Vector3.up * correction;
        }
        return instance;
    }

    static float GroundY(float x, float z)
    {
        return AshGroundSafety.SampleGroundHeight(new Vector3(x, 0f, z)) + 0.025f;
    }

    public static void Build(Transform root)
    {
        GameObject rocks = Load("EnvironmentModels/rocks/rock_moss_set_01/rock_moss_set_01");
        GameObject cliff = Load("EnvironmentModels/cliffs/rock_face_02/rock_face_02");
        GameObject trunk = Load("EnvironmentModels/deadwood/dead_tree_trunk_02/dead_tree_trunk_02");
        GameObject moss = Load("EnvironmentModels/groundcover/moss_01/moss_01");

        // Existing procedural trees remain in use; imported high-detail models are
        // reserved for rocks, cliffs, fallen wood and ground cover to keep mobile meshes light.

        // Put scanned assets in the opening camera view; previous placements were
        // so far sideways that the downloaded meshes were almost invisible at spawn.
        Vector3[] rockPositions = {
            new Vector3(-3.2f,0,2.8f), new Vector3(3.4f,0,4.6f),
            new Vector3(-4.8f,0,8.5f), new Vector3(5.1f,0,10.2f),
            new Vector3(-28,0,18), new Vector3(31,0,25),
            new Vector3(-37,0,42), new Vector3(39,0,49)
        };
        for (int i = 0; i < rockPositions.Length; i++)
        {
            Vector3 p = rockPositions[i];
            Place(rocks, root, "CC0 mossy boulder cluster " + i,
                new Vector3(p.x, GroundY(p.x,p.z), p.z), .32f + (i % 3) * .08f, i * 39f);
        }

        Vector3[] cliffPositions = {
            new Vector3(-10,0,14), new Vector3(11,0,17),
            new Vector3(-100,0,28), new Vector3(105,0,53)
        };
        for (int i = 0; i < cliffPositions.Length; i++)
        {
            Vector3 p = cliffPositions[i];
            Place(cliff, root, "CC0 weathered cliff rock " + i,
                new Vector3(p.x, GroundY(p.x,p.z), p.z), .42f + (i % 2) * .10f, i * 73f);
        }

        Vector3[] trunkPositions = {
            new Vector3(-6.5f,0,7.5f), new Vector3(7.2f,0,11.5f),
            new Vector3(-53,0,64), new Vector3(52,0,83)
        };
        for (int i = 0; i < trunkPositions.Length; i++)
        {
            Vector3 p = trunkPositions[i];
            Place(trunk, root, "CC0 fallen tree trunk " + i,
                new Vector3(p.x, GroundY(p.x,p.z), p.z), .75f + (i % 2) * .15f, i * 57f);
        }

        Vector3[] mossPositions = {
            new Vector3(-2.1f,0,2.4f), new Vector3(2.3f,0,3.5f),
            new Vector3(-4.2f,0,6.2f), new Vector3(4.4f,0,7.8f),
            new Vector3(-20,0,48), new Vector3(22,0,59)
        };
        for (int i = 0; i < mossPositions.Length; i++)
        {
            Vector3 p = mossPositions[i];
            Place(moss, root, "CC0 forest moss " + i,
                new Vector3(p.x, GroundY(p.x,p.z), p.z), .45f + (i % 3) * .12f, i * 53f);
        }

        AddSourceCredit();
        Debug.Log("[Ash Chronicles] Imported CC0 environment assets placed and grounded: " +
            "8 mossy rock clusters, 4 cliff rocks, 4 fallen trunks and 6 moss clumps.");
    }

    static void AddSourceCredit()
    {
        var canvasObject = new GameObject("Environment asset source credit");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        var labelObject = new GameObject("Poly Haven CC0 credit");
        labelObject.transform.SetParent(canvasObject.transform, false);
        var label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 13;
        label.color = new Color(.78f, .80f, .79f, .82f);
        label.alignment = TextAnchor.LowerLeft;
        label.text = "Environment assets: Poly Haven (CC0)";
        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(14f, 12f);
        rect.sizeDelta = new Vector2(320f, 22f);
    }
}
