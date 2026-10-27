using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Third-person follow camera with right-side swipe orbit controls for mobile.</summary>
[RequireComponent(typeof(Camera))]
public class AshFollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 5.5f, -8f);
    [Header("Touch look")]
    public float swipeSensitivity = 0.18f;
    public float minPitch = 8f;
    public float maxPitch = 65f;
    public float followSmooth = 12f;

    float yaw;
    float pitch = 32f;
    int lookFingerId = -1;
    bool mouseLooking;
    Vector3 currentOffset;
    Camera gameplayCamera;

    void Awake()
    {
        gameplayCamera = GetComponent<Camera>();
        // The world is generated at runtime, so no editor-authored camera settings
        // can be relied on. Explicitly enable rendering of every runtime world layer.
        gameplayCamera.enabled = true;
        gameplayCamera.cullingMask = ~0;
        gameplayCamera.nearClipPlane = 0.1f;
        gameplayCamera.farClipPlane = 500f;
        gameplayCamera.clearFlags = CameraClearFlags.SolidColor;
        gameplayCamera.backgroundColor = new Color(0.018f, 0.025f, 0.045f, 1f);
        if (gameplayCamera.tag != "MainCamera") gameplayCamera.tag = "MainCamera";
        Debug.Log("[Ash Chronicles] Gameplay camera configured: enabled=" + gameplayCamera.enabled +
            ", cullingMask=" + gameplayCamera.cullingMask + ", near=" + gameplayCamera.nearClipPlane +
            ", far=" + gameplayCamera.farClipPlane);
    }

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("[Ash Chronicles] Follow camera has no target; runtime world cannot be framed.");
            return;
        }

        Vector3 angles = Quaternion.LookRotation(-offset).eulerAngles;
        yaw = target.eulerAngles.y;
        pitch = Mathf.Clamp(Mathf.Atan2(offset.y, -offset.z) * Mathf.Rad2Deg, minPitch, maxPitch);
        currentOffset = Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -offset.magnitude);
        transform.position = target.position + currentOffset;
        transform.LookAt(target.position + Vector3.up * 1.15f);
        Debug.Log("[Ash Chronicles] Camera target=" + target.name + ", targetPosition=" + target.position +
            ", cameraPosition=" + transform.position);
    }

    void LateUpdate()
    {
        if (target == null) return;

        ReadLookInput();
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredOffset = orbit * new Vector3(0f, 0f, -offset.magnitude);
        currentOffset = Vector3.Lerp(currentOffset, desiredOffset, Time.deltaTime * followSmooth);
        transform.position = Vector3.Lerp(transform.position, target.position + currentOffset, Time.deltaTime * followSmooth);
        transform.LookAt(target.position + Vector3.up * 1.15f);
    }

    void ReadLookInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && lookFingerId == -1 &&
                touch.position.x >= Screen.width * 0.42f && !IsOverUI(touch.fingerId))
                lookFingerId = touch.fingerId;

            if (touch.fingerId != lookFingerId) continue;
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                yaw += touch.deltaPosition.x * swipeSensitivity;
                pitch = Mathf.Clamp(pitch - touch.deltaPosition.y * swipeSensitivity, minPitch, maxPitch);
            }
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                lookFingerId = -1;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
            mouseLooking = Input.mousePosition.x >= Screen.width * 0.42f && !IsOverUI(-1);
        if (Input.GetMouseButton(0) && mouseLooking)
        {
            yaw += Input.GetAxis("Mouse X") * 4f;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 4f, minPitch, maxPitch);
        }
        if (Input.GetMouseButtonUp(0)) mouseLooking = false;
#endif
    }

    bool IsOverUI(int fingerId)
    {
        if (EventSystem.current == null) return false;
        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}