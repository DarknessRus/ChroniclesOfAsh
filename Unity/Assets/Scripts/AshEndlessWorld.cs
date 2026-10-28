using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Endless, deterministic mobile-friendly adventure lane. Builds terrain, river,
/// props and enemies in front of the player, then releases chunks behind them.
/// Generation is local/offline; no network AI service is required.
/// </summary>
public sealed class AshEndlessWorld : MonoBehaviour
{
    public Transform player;
    public int seed = 7319;
    public float chunkLength = 48f;
    public int keepAhead = 4;
    public float removeBehind = 58f;

    const float HalfWidth = 54f;
    const int XSteps = 18;
    const int ZSteps = 24;
    const float TransitionStart = 150f;
    const float TransitionEnd = 205f;

    sealed class Chunk
    {
        public int index;
        public GameObject root;
        public float startZ;
    }

    readonly List<Chunk> chunks = new List<Chunk>();
    int nextChunk;
    bool oldWorldHidden;
    Transform chunkRoot;
    Material groundMat, waterMat, rockMat, barkMat, leafMat, crystalMat;

    public static float SampleGroundHeight(Vector3 p)
    {
        float x = Mathf.Clamp(p.x, -139.5f, 139.5f);
        float oldZ = Mathf.Clamp(p.z - 45f, -139.5f, 139.5f);
        float macro = Mathf.Sin(x * .021f + Mathf.Sin(oldZ * .013f) * .8f) * 5.2f
                    + Mathf.Cos(oldZ * .024f + x * .009f) * 4.4f;
        float rolling = (Mathf.PerlinNoise((x + 450f) * .018f, (oldZ + 450f) * .018f) - .5f) * 9.5f;
        float erosion = (Mathf.PerlinNoise((x + 90f) * .062f, (oldZ + 90f) * .062f) - .5f) * 2.4f
                      + (Mathf.PerlinNoise((x - 170f) * .15f, (oldZ + 270f) * .15f) - .5f) * .75f;
        float n = Mathf.PerlinNoise((x + 350f) * .010f, (oldZ + 350f) * .010f);
        float ridgeNoise = 1f - Mathf.Abs(n * 2f - 1f);
        float ridge = Mathf.Pow(Mathf.Clamp01((ridgeNoise - .48f) / .52f), 2f) * 24f;
        float axis = RiverX(oldZ);
        float distance = Mathf.Abs(x - axis);
        float shoulder = Mathf.Clamp01((distance - 10f) / 43f);
        float bank = Mathf.Clamp01((distance - 3.6f) / 8.5f);
        float upland = macro + rolling + erosion + ridge * shoulder;
        float oldHeight = Mathf.Lerp(-.08f, upland, bank);
        oldHeight -= Mathf.Exp(-Mathf.Pow(distance / 31f, 2f)) * 2.2f;
        oldHeight -= .15f;

        if (p.z <= TransitionStart) return oldHeight;

        float n1 = Mathf.PerlinNoise((p.x + 801.3f) * .025f, (p.z + 13.7f) * .018f);
        float n2 = Mathf.PerlinNoise((p.x - 113.8f) * .073f, (p.z + 401.2f) * .061f);
        float ridgeN = 1f - Mathf.Abs(Mathf.PerlinNoise((p.x + 87f) * .012f, (p.z + 912f) * .014f) * 2f - 1f);
        float hills = (n1 - .5f) * 17f + (n2 - .5f) * 5f
                    + Mathf.Pow(Mathf.Clamp01((ridgeN - .48f) / .52f), 2f) * 20f;
        float riverDist = Mathf.Abs(p.x - RiverX(p.z - 45f));
        float newHeight = hills * Mathf.Clamp01((riverDist - 9f) / 12f)
                        - Mathf.Exp(-Mathf.Pow(riverDist / 27f, 2f)) * 2.4f - .15f;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TransitionStart, TransitionEnd, p.z));
        return Mathf.Lerp(oldHeight, newHeight, blend);
    }

    static float RiverX(float z) { return Mathf.Sin(z * .027f) * 15f + Mathf.Sin(z * .071f) * 4f; }

    void Start()
    {
        if (player == null)
        {
            var controller = FindObjectOfType<AshPlayerController>();
            if (controller != null) player = controller.transform;
        }
        if (player == null) { Debug.LogWarning("[Ash Endless] Player not found; generator disabled."); enabled = false; return; }

        chunkRoot = new GameObject("Endless Generated Chunks").transform;
        chunkRoot.SetParent(transform, false);
        groundMat = MakeMat(new Color(.20f,.22f,.19f), .92f);
        waterMat = MakeMat(new Color(.035f,.16f,.22f), .22f);
        rockMat = MakeMat(new Color(.20f,.21f,.22f), .95f);
        barkMat = MakeMat(new Color(.16f,.105f,.07f), .9f);
        leafMat = MakeMat(new Color(.10f,.17f,.12f), .88f);
        crystalMat = MakeMat(new Color(.15f,.55f,.72f), .28f, true);
        nextChunk = Mathf.FloorToInt((player.position.z + 110f) / chunkLength);
        for (int i = 0; i < keepAhead + 2; i++) CreateNextChunk();
    }

    Material MakeMat(Color color, float smoothness, bool emissive = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (emissive && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.6f);
        }
        return mat;
    }

    void Update()
    {
        if (player == null) return;
        float targetZ = player.position.z + keepAhead * chunkLength;
        while (nextChunk * chunkLength < targetZ) CreateNextChunk();

        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            if (chunks[i].startZ + chunkLength < player.position.z - removeBehind)
            {
                if (chunks[i].root != null) Destroy(chunks[i].root);
                chunks.RemoveAt(i);
            }
        }

        // The original hand-built valley is the starting biome. Once the player
        // leaves it, retire its finite scenery while the endless chunks take over.
        if (!oldWorldHidden && player.position.z > TransitionEnd + 5f)
        {
            oldWorldHidden = true;
            Transform oldTerrain = transform.Find("Open World - sculpted ash valley");
            if (oldTerrain != null)
            {
                var renderer = oldTerrain.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
                var collider = oldTerrain.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }
            Transform oldRiver = transform.Find("Winding river - cold reflective water");
            if (oldRiver != null) oldRiver.gameObject.SetActive(false);
            Debug.Log("[Ash Endless] Starting valley retired; endless terrain now active.");
        }

        // Retire old static scenery behind the player, but never touch the player,
        // the manager, the starting ground mesh, or generated chunks.
        if (player.position.z > 220f)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == chunkRoot || child.name == "Open World - sculpted ash valley" ||
                    child.name == "Ground safety floor" || child.name == "Winding river - cold reflective water")
                    continue;
                if (child.position.z < player.position.z - removeBehind)
                    Destroy(child.gameObject);
            }
        }
    }

    void CreateNextChunk()
    {
        int index = nextChunk++;
        float startZ = index * chunkLength;
        var root = new GameObject("Endless Chunk " + index);
        root.transform.SetParent(chunkRoot, false);
        root.transform.position = Vector3.zero;
        BuildGround(root.transform, startZ, index);
        BuildRiver(root.transform, startZ, index);
        ScatterProps(root.transform, startZ, index);
        SpawnEnemies(root.transform, startZ, index);
        chunks.Add(new Chunk { index = index, root = root, startZ = startZ });
    }

    void BuildGround(Transform parent, float startZ, int index)
    {
        var mesh = new Mesh { name = "Endless terrain chunk " + index };
        int count = (XSteps + 1) * (ZSteps + 1);
        var vertices = new Vector3[count];
        var uv = new Vector2[count];
        var tris = new int[XSteps * ZSteps * 6];
        for (int z = 0; z <= ZSteps; z++)
        for (int x = 0; x <= XSteps; x++)
        {
            float wx = Mathf.Lerp(-HalfWidth, HalfWidth, x / (float)XSteps);
            float wz = startZ + chunkLength * z / ZSteps;
            int id = z * (XSteps + 1) + x;
            vertices[id] = new Vector3(wx, SampleGroundHeight(new Vector3(wx, 0, wz)), wz);
            uv[id] = new Vector2(wx * .16f, wz * .16f);
            if (x < XSteps && z < ZSteps)
            {
                int t = (z * XSteps + x) * 6;
                tris[t] = id; tris[t+1] = id + XSteps + 1; tris[t+2] = id + 1;
                tris[t+3] = id + 1; tris[t+4] = id + XSteps + 1; tris[t+5] = id + XSteps + 2;
            }
        }
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = tris;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject("Terrain");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = groundMat;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    void BuildRiver(Transform parent, float startZ, int index)
    {
        const int steps = 24;
        var verts = new Vector3[(steps + 1) * 2];
        var tris = new int[steps * 6];
        for (int i = 0; i <= steps; i++)
        {
            float z = startZ + chunkLength * i / steps;
            float x = RiverX(z - 45f);
            float leftX = x - 3.5f, rightX = x + 3.5f;
            verts[i * 2] = new Vector3(leftX, SampleGroundHeight(new Vector3(leftX,0,z)) + .07f, z);
            verts[i * 2 + 1] = new Vector3(rightX, SampleGroundHeight(new Vector3(rightX,0,z)) + .07f, z);
            if (i < steps)
            {
                int t = i * 6, a = i * 2;
                tris[t] = a; tris[t+1] = a+2; tris[t+2] = a+1;
                tris[t+3] = a+1; tris[t+4] = a+2; tris[t+5] = a+3;
            }
        }
        var mesh = new Mesh { name = "Endless river " + index };
        mesh.vertices = verts; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var water = new GameObject("River");
        water.transform.SetParent(parent, false);
        water.AddComponent<MeshFilter>().sharedMesh = mesh;
        water.AddComponent<MeshRenderer>().sharedMaterial = waterMat;
    }

    void ScatterProps(Transform parent, float startZ, int index)
    {
        var rng = new System.Random(seed ^ (index * 7919));
        // Keep the center corridor traversable; scenery density and biome tint vary by chunk.
        int trees = 13 + rng.Next(0, 9);
        for (int i = 0; i < trees; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * 45f;
            float z = startZ + 4f + (float)rng.NextDouble() * (chunkLength - 8f);
            float riverX = RiverX(z - 45f);
            if (Mathf.Abs(x) < 6f || Mathf.Abs(x - riverX) < 7f) x += x < 0 ? -9f : 9f;
            float y = SampleGroundHeight(new Vector3(x, 0, z));
            float scale = .8f + (float)rng.NextDouble() * 1.7f;
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Procedural dead pine trunk";
            trunk.transform.SetParent(parent, false);
            trunk.transform.position = new Vector3(x, y + 1.25f * scale, z);
            trunk.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
            trunk.transform.localScale = new Vector3(.20f * scale, 1.25f * scale, .20f * scale);
            trunk.GetComponent<Renderer>().sharedMaterial = barkMat;
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Ash pine crown";
            crown.transform.SetParent(parent, false);
            crown.transform.position = new Vector3(x, y + 2.8f * scale, z);
            crown.transform.localScale = new Vector3(1.45f * scale, 2.0f * scale, 1.45f * scale);
            crown.GetComponent<Renderer>().sharedMaterial = (index % 4 == 2) ? crystalMat : leafMat;
            if (index % 5 == 3 && i % 4 == 0)
            {
                var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = "Procedural rune stone";
                stone.transform.SetParent(parent, false);
                stone.transform.position = new Vector3(x + 1.4f, y + .45f, z + 1f);
                stone.transform.localScale = Vector3.one * .8f;
                stone.GetComponent<Renderer>().sharedMaterial = crystalMat;
            }
        }
        // Scattered angular rocks on both sides of the path.
        for (int i = 0; i < 10; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * 46f;
            if (Mathf.Abs(x) < 7f) x = x < 0 ? -8f : 8f;
            float z = startZ + (float)rng.NextDouble() * chunkLength;
            float y = SampleGroundHeight(new Vector3(x, 0, z));
            float s = .5f + (float)rng.NextDouble() * 1.8f;
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Procedural ash boulder";
            rock.transform.SetParent(parent, false);
            rock.transform.position = new Vector3(x, y + s * .38f, z);
            rock.transform.localScale = new Vector3(s * 1.2f, s * .7f, s);
            rock.GetComponent<Renderer>().sharedMaterial = rockMat;
        }
    }

    void SpawnEnemies(Transform parent, float startZ, int index)
    {
        var rng = new System.Random((seed + 17) ^ (index * 104729));
        int count = 1 + rng.Next(0, 3);
        for (int i = 0; i < count; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * 10f;
            if (Mathf.Abs(x) < 4f) x = x < 0 ? -5f : 5f;
            float z = startZ + 12f + i * 10f + (float)rng.NextDouble() * 5f;
            float y = SampleGroundHeight(new Vector3(x, 0, z));
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Ash Wraith - Endless " + index + "-" + i;
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = new Vector3(x, y + .95f, z);
            enemy.transform.localScale = new Vector3(.78f, 1f, .78f);
            enemy.GetComponent<Renderer>().sharedMaterial = index % 3 == 0 ? crystalMat : rockMat;
            var capsule = enemy.GetComponent<CapsuleCollider>();
            if (capsule != null) capsule.isTrigger = false;
            enemy.AddComponent<AshEnemy>();
            var ai = enemy.AddComponent<AshEndlessEnemyAI>();
            ai.player = player;
            ai.roamRadius = 5f + (float)rng.NextDouble() * 5f;
        }
    }
}

/// <summary>Small offline enemy AI: patrol, detect, pursue, and close-distance attack.</summary>
public sealed class AshEndlessEnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 2.1f;
    public float detectRange = 18f;
    public float attackRange = 1.7f;
    public float roamRadius = 6f;

    Vector3 home;
    Vector3 roamTarget;
    float nextRoam;
    float nextAttack;
    float phase;

    void Start()
    {
        home = transform.position;
        PickRoamTarget();
        phase = Random.value * 10f;
    }

    void Update()
    {
        if (player == null)
        {
            var p = FindObjectOfType<AshPlayerController>();
            if (p != null) player = p.transform;
            if (player == null) return;
        }
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        Vector3 direction = Vector3.zero;
        if (distance < detectRange && distance > attackRange)
            direction = toPlayer.normalized;
        else if (distance >= detectRange)
        {
            if (Time.time >= nextRoam || Vector3.Distance(transform.position, roamTarget) < .8f)
                PickRoamTarget();
            Vector3 roam = roamTarget - transform.position;
            roam.y = 0;
            if (roam.sqrMagnitude > .3f) direction = roam.normalized * .55f;
        }

        if (direction.sqrMagnitude > .01f)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 7f * Time.deltaTime);
        }
        Vector3 p = transform.position;
        p.y = AshEndlessWorld.SampleGroundHeight(p) + .95f + Mathf.Sin(Time.time * 3f + phase) * .035f;
        transform.position = p;

        if (distance <= attackRange && Time.time >= nextAttack)
        {
            nextAttack = Time.time + 1.6f;
            // The prototype has no player-health component yet. Keep the attack telegraph visible
            // without applying invisible damage; the existing combat system can still kill enemies.
            transform.localScale = new Vector3(.92f, 1.08f, .92f);
        }
        else if (Time.time > nextAttack - .3f)
            transform.localScale = new Vector3(.78f, 1f, .78f);
    }

    void PickRoamTarget()
    {
        Vector2 circle = Random.insideUnitCircle * roamRadius;
        roamTarget = home + new Vector3(circle.x, 0, circle.y);
        nextRoam = Time.time + Random.Range(2f, 5f);
    }
}
