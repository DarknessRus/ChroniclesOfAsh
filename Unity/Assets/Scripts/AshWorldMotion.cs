using UnityEngine;

/// <summary>
/// Runtime skeletal animation for the current assembled hero, plus tree and river motion.
/// The armor meshes are attached to named bone transforms and animated by looping clips.
/// This avoids the previous per-part procedural offsets and works without external packages.
/// </summary>
public sealed class AshWorldMotion : MonoBehaviour
{
    public enum MotionKind { Hero, Tree, Water }
    public MotionKind kind;
    public float phase;

    CharacterController heroController;
    Animation heroAnimation;
    MeshFilter waterFilter;
    Vector2[] originalUV;
    Mesh waterMesh;
    Renderer waterRenderer;
    Vector2 waterOffset;
    string activeClip = "";
    string idleClipName = "Idle";
    string walkClipName = "Walk";
    string runClipName = "Run";

    void Start()
    {
        if (kind == MotionKind.Hero)
        {
            heroController = GetComponent<CharacterController>();
            // Imported FBX exposes legacy Animation clips from the real skinned rig.
            // Search children because Unity may place the Animation component on the FBX root.
            heroAnimation = GetComponentInChildren<Animation>(true);
            if (heroAnimation != null && FindClipContaining("Walk") != null && FindClipContaining("Run") != null)
            {
                idleClipName = FindClipContaining("Idle") != null ? FindClipContaining("Idle") : heroAnimation.clip.name;
                walkClipName = FindClipContaining("Walk");
                runClipName = FindClipContaining("Run");
                heroAnimation.playAutomatically = false;
                heroAnimation.wrapMode = WrapMode.Loop;
                heroAnimation.Play(idleClipName);
                activeClip = idleClipName;
                Debug.Log("[Ash Chronicles] External skinned rig active. Clips: " + idleClipName + ", " + walkClipName + ", " + runClipName);
            }
            else
            {
                BuildSkeletalRigAndClips();
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

    string FindClipContaining(string token)
    {
        if (heroAnimation == null) return null;
        foreach (AnimationState state in heroAnimation)
            if (state.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return state.name;
        return null;
    }

    void BuildSkeletalRigAndClips()
    {
        // The prototype hero is assembled from armor pieces. Convert those pieces into
        // a named bone hierarchy so motion comes from joint rotations, not moving each
        // mesh independently as the previous implementation did.
        Transform oldRig = transform.Find("Skeleton");
        if (oldRig != null) Destroy(oldRig.gameObject);

        var skeletonObject = new GameObject("Skeleton");
        Transform skeleton = skeletonObject.transform;
        skeleton.SetParent(transform, false);
        skeleton.localPosition = Vector3.zero;
        skeleton.localRotation = Quaternion.identity;
        skeleton.localScale = Vector3.one;

        Transform torso = MakeBone(skeleton, "Spine", new Vector3(0f, .08f, 0f));
        Transform head = MakeBone(skeleton, "Head", new Vector3(0f, .62f, .02f));
        Transform leftArm = MakeBone(skeleton, "LeftArm", new Vector3(-.38f, .34f, 0f));
        Transform rightArm = MakeBone(skeleton, "RightArm", new Vector3(.38f, .34f, 0f));
        Transform leftLeg = MakeBone(skeleton, "LeftLeg", new Vector3(-.16f, -.36f, 0f));
        Transform rightLeg = MakeBone(skeleton, "RightLeg", new Vector3(.16f, -.36f, 0f));
        Transform cloak = MakeBone(skeleton, "Cloak", new Vector3(0f, .05f, -.22f));

        ReparentNamedParts(torso, "Armored torso", "Chest plate", "Chest crest",
            "Crossed leather strap", "Waist belt", "Belt buckle");
        ReparentNamedParts(head, "Face", "Eye shadow", "Eye glint", "Hood crown",
            "Hood shadow", "Helmet crest", "Helmet horn", "Ranger hood mantle",
            "Necromancer bone charm");
        // Keep left/right limb assignment in AttachMatching; some armor piece names
        // (Boot, Knee guard and Forearm bracer) are intentionally shared by both sides.
        ReparentNamedParts(cloak, "Weathered cloak", "Cloak torn edge");

        // Name-based grouping above can include repeated generic names. Correctly sort
        // all remaining arm/leg/shoulder pieces by their original left/right names.
        AttachMatching(skeleton, leftArm, rightArm, leftLeg, rightLeg, cloak, torso, head);

        heroAnimation = GetComponent<Animation>();
        if (heroAnimation == null) heroAnimation = gameObject.AddComponent<Animation>();
        heroAnimation.playAutomatically = false;
        heroAnimation.wrapMode = WrapMode.Loop;

        AnimationClip idle = MakeClip("Idle", 2f, 0f);
        AnimationClip walk = MakeClip("Walk", 1.0f, 28f);
        AnimationClip run = MakeClip("Run", .62f, 48f);
        heroAnimation.AddClip(idle, "Idle");
        heroAnimation.AddClip(walk, "Walk");
        heroAnimation.AddClip(run, "Run");
        heroAnimation.clip = idle;
        heroAnimation.Play("Idle");
        activeClip = "Idle";

        Debug.Log("[Ash Chronicles] Skeletal rig ready: 7 named bones; Idle, Walk and Run animation clips created.");
    }

    Transform MakeBone(Transform parent, string boneName, Vector3 localPosition)
    {
        var go = new GameObject(boneName);
        Transform bone = go.transform;
        bone.SetParent(parent, false);
        bone.localPosition = localPosition;
        bone.localRotation = Quaternion.identity;
        return bone;
    }

    void ReparentNamedParts(Transform bone, params string[] exactNames)
    {
        // Move direct children with matching names under a joint, preserving their
        // current world transforms so the armor silhouette does not jump at startup.
        Transform[] parts = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            Transform part = parts[i];
            if (part == transform || part == bone || part.IsChildOf(bone) ||
                part.name == "Skeleton" || part.name == "Spine" || part.name == "Head" ||
                part.name == "LeftArm" || part.name == "RightArm" || part.name == "LeftLeg" ||
                part.name == "RightLeg" || part.name == "Cloak")
                continue;
            bool match = false;
            for (int n = 0; n < exactNames.Length; n++)
                if (part.name == exactNames[n]) { match = true; break; }
            if (match && part.parent == transform)
                part.SetParent(bone, true);
        }
    }

    void AttachMatching(Transform skeleton, Transform leftArm, Transform rightArm,
        Transform leftLeg, Transform rightLeg, Transform cloak, Transform torso, Transform head)
    {
        // Any pieces not picked up by exact-name groups are attached according to their
        // authored side/name. This includes pauldrons, boots and torn cloak panels.
        Transform[] parts = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            Transform p = parts[i];
            if (p == transform || p == skeleton || p == leftArm || p == rightArm ||
                p == leftLeg || p == rightLeg || p == cloak || p == torso || p == head ||
                p.IsChildOf(skeleton) || p.parent != transform) continue;
            string n = p.name.ToLowerInvariant();
            if (n.Contains("armored arm") || n.Contains("forearm bracer") ||
                n.Contains("shoulder pauldron"))
                p.SetParent(n.Contains("left") || p.localPosition.x < 0f ? leftArm : rightArm, true);
            else if (n.Contains("greave") || n.Contains("boot") || n.Contains("knee guard"))
                p.SetParent(n.Contains("left") || p.localPosition.x < 0f ? leftLeg : rightLeg, true);
            else if (n.Contains("cloak") || n.Contains("torn edge"))
                p.SetParent(cloak, true);
            else if (n.Contains("face") || n.Contains("eye") || n.Contains("hood") || n.Contains("helmet"))
                p.SetParent(head, true);
            else
                p.SetParent(torso, true);
        }
    }

    AnimationClip MakeClip(string clipName, float duration, float strideDegrees)
    {
        var clip = new AnimationClip();
        clip.name = clipName;
        clip.legacy = true;
        clip.frameRate = 30f;
        clip.wrapMode = WrapMode.Loop;
        string[] bones = { "LeftArm", "RightArm", "LeftLeg", "RightLeg" };
        for (int i = 0; i < bones.Length; i++)
        {
            float sign = (i == 0 || i == 3) ? 1f : -1f;
            float amplitude = (i < 2) ? strideDegrees * .72f : strideDegrees;
            string path = "Skeleton/" + bones[i];
            clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.x",
                LoopingCurve(duration, amplitude * sign));
        }
        float torsoAmount = clipName == "Idle" ? 1.8f : clipName == "Walk" ? 2.5f : 4f;
        clip.SetCurve("Skeleton/Spine", typeof(Transform), "localEulerAnglesRaw.z",
            new AnimationCurve(
                new Keyframe(0f, -torsoAmount), new Keyframe(duration * .25f, 0f),
                new Keyframe(duration * .5f, torsoAmount), new Keyframe(duration * .75f, 0f),
                new Keyframe(duration, -torsoAmount)));
        float cloakAmount = clipName == "Idle" ? 1f : clipName == "Walk" ? 7f : 13f;
        clip.SetCurve("Skeleton/Cloak", typeof(Transform), "localEulerAnglesRaw.x",
            new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(duration * .25f, cloakAmount),
                new Keyframe(duration * .5f, 0f), new Keyframe(duration * .75f, -cloakAmount),
                new Keyframe(duration, 0f)));
        float headAmount = clipName == "Idle" ? 1.5f : 0.8f;
        clip.SetCurve("Skeleton/Head", typeof(Transform), "localEulerAnglesRaw.z",
            new AnimationCurve(
                new Keyframe(0f, -headAmount), new Keyframe(duration * .5f, headAmount),
                new Keyframe(duration, -headAmount)));
        return clip;
    }

    AnimationCurve LoopingCurve(float duration, float amplitude)
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(duration * .25f, amplitude),
            new Keyframe(duration * .5f, 0f),
            new Keyframe(duration * .75f, -amplitude),
            new Keyframe(duration, 0f));
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
        else if (kind == MotionKind.Hero && heroAnimation != null)
        {
            Vector3 planarVelocity = heroController != null
                ? new Vector3(heroController.velocity.x, 0f, heroController.velocity.z)
                : Vector3.zero;
            float speed = planarVelocity.magnitude;
            string wanted = speed < .12f ? idleClipName : speed < 3.15f ? walkClipName : runClipName;
            if (wanted != activeClip && heroAnimation.GetClip(wanted) != null)
            {
                heroAnimation.CrossFade(wanted, .12f);
                activeClip = wanted;
            }
        }
    }
}
