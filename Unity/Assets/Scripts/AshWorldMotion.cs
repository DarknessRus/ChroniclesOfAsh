using UnityEngine;

/// <summary>
/// Lightweight, visible mobile motion for the procedural hero, tree crowns and river.
/// Hero parts are animated by their authored names and offsets; no external Animator is required.
/// </summary>
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
    CharacterController heroController;

    void Start()
    {
        if (kind == MotionKind.Hero)
        {
            heroController = GetComponent<CharacterController>();
            // Traverse the complete hierarchy so grouped or nested parts animate too.
            movingParts = GetComponentsInChildren<Transform>(true);
            baseRotations = new Quaternion[movingParts.Length];
            basePositions = new Vector3[movingParts.Length];
            for (int i = 0; i < movingParts.Length; i++)
            {
                baseRotations[i] = movingParts[i].localRotation;
                basePositions[i] = movingParts[i].localPosition;
            }
            Debug.Log("[Ash Chronicles] Hero animation initialized for " + movingParts.Length + " transforms.");
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
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.ToLowerInvariant().Contains("boughs"))
                    child.localRotation = Quaternion.Euler(Mathf.Sin(t * .72f) * 1.8f, 0f,
                        Mathf.Sin(t * .91f) * 1.25f);
            }
        }
        else if (kind == MotionKind.Water && waterMesh != null && originalUV != null)
        {
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
            Vector3 planarVelocity = heroController != null
                ? new Vector3(heroController.velocity.x, 0f, heroController.velocity.z)
                : Vector3.zero;
            float speed = planarVelocity.magnitude;
            float amount = Mathf.Clamp01(speed / 1.8f);
            // A pronounced stride remains visible on a small phone screen.
            float cadence = t * Mathf.Lerp(5.2f, 9.0f, amount);
            float stride = Mathf.Sin(cadence) * amount;
            float oppositeStride = Mathf.Sin(cadence + Mathf.PI) * amount;
            float breathe = Mathf.Sin(t * 2.1f) * .018f;
            float idleSway = Mathf.Sin(t * 1.35f) * .025f;
            for (int i = 0; i < movingParts.Length; i++)
            {
                Transform p = movingParts[i];
                if (p == null) continue;
                string n = p.name.ToLowerInvariant();
                p.localPosition = basePositions[i];
                p.localRotation = baseRotations[i];

                float side = basePositions[i].x < -.025f ? -1f : 1f;
                if (n.Contains("armored arm") || n.Contains("shoulder pauldron"))
                {
                    p.localRotation = baseRotations[i] * Quaternion.Euler(stride * 52f, 0f, -side * Mathf.Abs(stride) * 9f);
                    p.localPosition += Vector3.forward * stride * .07f;
                }
                else if (n.Contains("forearm bracer"))
                {
                    p.localRotation = baseRotations[i] * Quaternion.Euler(stride * 42f, 0f, -side * Mathf.Abs(stride) * 7f);
                }
                else if (n.Contains("greave") || n.Contains("boot") || n.Contains("knee guard"))
                {
                    float legStride = side < 0f ? stride : oppositeStride;
                    p.localPosition += Vector3.forward * legStride * .24f;
                    p.localPosition += Vector3.up * Mathf.Abs(legStride) * .045f;
                    p.localRotation = baseRotations[i] * Quaternion.Euler(legStride * 42f, 0f, 0f);
                }
                else if (n.Contains("cloak") || n.Contains("torn edge"))
                {
                    p.localRotation = baseRotations[i] * Quaternion.Euler(Mathf.Abs(stride) * 16f, 0f, stride * 18f);
                }
                else if (n.Contains("torso") || n.Contains("chest plate") || n.Contains("chest crest") ||
                         n.Contains("crossed leather strap") || n.Contains("waist belt") || n.Contains("belt buckle"))
                {
                    p.localPosition += Vector3.up * (breathe + Mathf.Abs(stride) * .035f);
                    p.localRotation = baseRotations[i] * Quaternion.Euler(-Mathf.Abs(stride) * 5f, 0f, idleSway * 22f);
                }
                else if (n.Contains("face") || n.Contains("eye shadow") || n.Contains("eye glint") ||
                         n.Contains("hood") || n.Contains("helmet"))
                {
                    p.localPosition += Vector3.up * breathe;
                    p.localRotation = baseRotations[i] * Quaternion.Euler(Mathf.Sin(t * 2.1f) * 2.5f, idleSway * 18f, 0f);
                }
            }
        }
    }
}