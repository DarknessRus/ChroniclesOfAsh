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
            movingParts = new Transform[transform.childCount];
            baseRotations = new Quaternion[transform.childCount];
            basePositions = new Vector3[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                movingParts[i] = transform.GetChild(i);
                baseRotations[i] = movingParts[i].localRotation;
                basePositions[i] = movingParts[i].localPosition;
            }
            Debug.Log("[Ash Chronicles] Procedural hero animation initialized for " + movingParts.Length + " visible parts.");
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
            float amount = Mathf.Clamp01(speed / 2.4f);
            // A larger, slower stride stays readable on a phone screen.
            float stride = Mathf.Sin(t * Mathf.Lerp(5.5f, 10.5f, amount)) * amount;
            float oppositeStride = Mathf.Sin(t * Mathf.Lerp(5.5f, 10.5f, amount) + Mathf.PI) * amount;
            float breathe = Mathf.Sin(t * 2.1f) * .012f;
            for (int i = 0; i < movingParts.Length; i++)
            {
                Transform p = movingParts[i];
                if (p == null) continue;
                string n = p.name.ToLowerInvariant();
                p.localPosition = basePositions[i];
                p.localRotation = baseRotations[i];

                float side = basePositions[i].x < -.025f ? -1f : 1f;
                if (n.Contains("armored arm"))
                {
                    p.localRotation = baseRotations[i] * Quaternion.Euler(stride * 34f, 0f, -side * Mathf.Abs(stride) * 5f);
                }
                else if (n.Contains("forearm bracer"))
                {
                    p.localRotation = baseRotations[i] * Quaternion.Euler(stride * 27f, 0f, -side * Mathf.Abs(stride) * 4f);
                }
                else if (n.Contains("greave") || n.Contains("boot") || n.Contains("knee guard"))
                {
                    float legStride = side < 0f ? stride : oppositeStride;
                    p.localPosition += Vector3.forward * legStride * .12f;
                    p.localPosition += Vector3.up * Mathf.Abs(legStride) * .025f;
                    p.localRotation = baseRotations[i] * Quaternion.Euler(legStride * 24f, 0f, 0f);
                }
                else if (n.Contains("cloak") || n.Contains("torn edge"))
                {
                    p.localRotation = baseRotations[i] * Quaternion.Euler(Mathf.Abs(stride) * 8f, 0f, stride * 7f);
                }
                else if (n.Contains("torso") || n.Contains("chest plate") || n.Contains("chest crest") ||
                         n.Contains("crossed leather strap") || n.Contains("waist belt") || n.Contains("belt buckle"))
                {
                    p.localPosition += Vector3.up * (breathe + Mathf.Abs(stride) * .018f);
                    p.localRotation = baseRotations[i] * Quaternion.Euler(-Mathf.Abs(stride) * 3f, 0f, 0f);
                }
                else if (n.Contains("face") || n.Contains("eye shadow") || n.Contains("eye glint") ||
                         n.Contains("hood") || n.Contains("helmet"))
                {
                    p.localPosition += Vector3.up * breathe;
                    p.localRotation = baseRotations[i] * Quaternion.Euler(Mathf.Sin(t * 2.1f) * 1.5f, 0f, 0f);
                }
            }
        }
    }
}