using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AshPlayerController : MonoBehaviour
{
    public float moveSpeed = 4.2f;
    public float gravity = -18f;

    CharacterController cc;
    Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.skinWidth = 0.08f;
        cc.stepOffset = 0.3f;
        cc.minMoveDistance = 0f;
        AshGroundSafety.PrepareWorld(transform);
    }

    void Update()
    {
        Vector2 touch = AshVirtualJoystick.InputVector;
        float h = Mathf.Abs(touch.x) > 0.08f ? touch.x : Input.GetAxisRaw("Horizontal");
        float v = Mathf.Abs(touch.y) > 0.08f ? touch.y : Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 move = CameraRelative(input);
        if (move.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 12f * Time.deltaTime);

        cc.Move(move * moveSpeed * Time.deltaTime);

        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        // Backup grounding follows the same height function used to build the visible terrain.
        // This prevents a missed thin-mesh collision from sending the player below the world.
        float groundY = AshGroundSafety.SampleGroundHeight(transform.position);
        float footY = transform.position.y - cc.height * 0.5f;
        if (footY < groundY + 0.035f)
        {
            Vector3 groundedPosition = transform.position;
            groundedPosition.y = groundY + cc.height * 0.5f + 0.035f;
            transform.position = groundedPosition;
            if (velocity.y < 0f) velocity.y = 0f;
        }

        if (AshCombatButton.AttackPressed)
        {
            AshCombatButton.AttackPressed = false;
            var hits = Physics.OverlapSphere(transform.position + transform.forward * 1.35f + Vector3.up * 0.6f, 1.45f);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponentInParent<AshEnemy>();
                if (enemy != null) enemy.TakeDamage(25f);
            }
        }
    }

    Vector3 CameraRelative(Vector3 input)
    {
        var cam = Camera.main;
        if (cam == null) return input;
        Vector3 f = cam.transform.forward;
        f.y = 0;
        f.Normalize();
        Vector3 r = cam.transform.right;
        r.y = 0;
        r.Normalize();
        return (f * input.z + r * input.x).normalized;
    }
}