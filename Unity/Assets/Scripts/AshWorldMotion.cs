using UnityEngine;

/// <summary>Lightweight mobile animation for the procedural player, pine crowns and river surface.</summary>
public sealed class AshWorldMotion : MonoBehaviour
{
    public enum MotionKind { Hero, Tree, Water }
    public MotionKind kind;
    public float phase;
    Transform[] movingParts;
    Quaternion[] baseRotations;
    Vector3[] basePositions;
    MeshFilter waterFilter;
    Vector2[] originalUV;
    Mesh waterMesh;
    Renderer waterRenderer;
    Vector2 waterOffset;

    void Awake()
    {
        if (kind == MotionKind.Hero)
        {
            movingParts = new Transform[transform.childCount];
            baseRotations = new Quaternion[transform.childCount];
            basePositions = new Vector3[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                movingParts[i] = transform.GetChild(i);
                baseRotations[i] = movingParts[i].localRotation;
                basePositions[i] = movingParts[i].localPosition;
            }
        }
        else if (kind == MotionKind.Water)
        {
            waterFilter = GetComponent<MeshFilter>();
            waterRenderer = GetComponent<Renderer>();
            if (waterFilter != null && waterFilter.sharedMesh != null)
            {
                waterMesh = waterFilter.mesh;
                originalUV = waterMesh.uv;
            }
        }
    }

    void Update()
    {
        float t = Time.time + phase;
        if (kind == MotionKind.Tree)
        {
            // Keep trunks rooted; sway only the flexible upper crown.
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.Contains("boughs"))
                {
                    child.localRotation = Quaternion.Euler(
                        Mathf.Sin(t * .72f) * 1.8f,
                        0f,
                        Mathf.Sin(t * .91f) * 1.25f);
                }
            }
        }
        else if (kind == MotionKind.Water && waterMesh != null && originalUV != null)
        {
            // Two low-amplitude wave directions create visible flow without a shader dependency.
            Vector2[] uv = waterMesh.uv;
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i].x = originalUV[i].x + t * .035f;
                uv[i].y = originalUV[i].y + Mathf.Sin(originalUV[i].x * 2.2f + t * 1.4f) * .012f;
            }
            waterMesh.uv = uv;
            if (waterRenderer != null && waterRenderer.sharedMaterial != null)
            {
                waterOffset.x = Mathf.Repeat(t * .018f, 1f);
                waterOffset.y = Mathf.Repeat(t * .011f, 1f);
                waterRenderer.sharedMaterial.mainTextureOffset = waterOffset;
            }
        }
        else if (kind == MotionKind.Hero && movingParts != null)
        {
            CharacterController controller = GetComponent<CharacterController>();
            float speed = controller != null ? new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude : 0f;
            float amount = Mathf.Clamp01(speed / 2.4f);
            float stride = Mathf.Sin(t * 9.5f) * amount;
            for (int i = 0; i < movingParts.Length; i++)
            {
                Transform p = movingParts[i];
                if (p == null) continue;
                string n = p.name.ToLowerInvariant();
                p.localPosition = basePositions[i];
                p.localRotation = baseRotations[i];
                if (n.Contains("armored arm") || n.Contains("forearm bracer"))
                {
                    float side = n.Contains("left") ? -1f : 1f;
                    p.localRotation = baseRotations[i] * Quaternion.Euler(stride * 22f, 0f, side * stride * 8f);
                }
                else if (n.Contains("greave") || n.Contains("boot") || n.Contains("knee guard"))
                {
                    float side = n.Contains("left") ? -1f : 1f;
                    p.localPosition += Vector3.forward * stride * side * .055f;
                    p.localRotation = baseRotations[i] * Quaternion.Euler(stride * side * 12f, 0f, 0f);
                }
                else if (n.Contains("cloak") || n.Contains("torn edge"))
                {
                    p.localRotation = baseRotations[i] * Quaternion.Euler(Mathf.Abs(stride) * 5f, 0f, stride * 4f);
                }
            }
        }
    }
}
