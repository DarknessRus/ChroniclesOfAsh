using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Third-person follow camera with right-side swipe orbit controls for mobile.</summary>
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

    void Start()
    {
        Vector3 angles = Quaternion.LookRotation(-offset).eulerAngles;
        yaw = target != null ? target.eulerAngles.y : angles.y;
        pitch = Mathf.Clamp(Mathf.Atan2(offset.y, -offset.z) * Mathf.Rad2Deg, minPitch, maxPitch);
        currentOffset = Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -offset.magnitude);
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
        // Touches beginning on the right half control the camera. The left half remains
        // dedicated to movement; UI buttons are excluded so attacks do not rotate the view.
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && lookFingerId == -1 &&
                touch.position.x >= Screen.width * 0.42f &&
                !IsOverUI(touch.fingerId))
            {
                lookFingerId = touch.fingerId;
            }

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
