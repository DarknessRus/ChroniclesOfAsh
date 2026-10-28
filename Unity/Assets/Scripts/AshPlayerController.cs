using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AshPlayerController : MonoBehaviour
{
    public float walkSpeed = 2.4f;
    public float runSpeed = 4.2f;
    public float gravity = -18f;

    CharacterController cc;
    Vector3 velocity;

    const float GroundClearance = 0.04f;

    void Awake()
    {
        // A primitive capsule already has a CapsuleCollider. Do not leave it stacked
        // with CharacterController; the controller must be the sole player collider.
        var primitiveCollider = GetComponent<CapsuleCollider>();
        if (primitiveCollider != null) primitiveCollider.enabled = false;

        cc = GetComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = Vector3.zero;
        cc.skinWidth = 0.04f;
        cc.stepOffset = 0.3f;
        cc.minMoveDistance = 0f;
        cc.detectCollisions = true;

        AshGroundSafety.PrepareWorld(transform);
        SnapToGround(true);
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

        // Analog joystick tilt selects a walk at low input and a run when pushed far.
        // Keyboard/digital input remains full-speed for convenient desktop testing.
        float currentSpeed = input.magnitude >= 0.78f ? runSpeed : walkSpeed;
        cc.Move(move * currentSpeed * Time.deltaTime);

        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        // The procedural terrain is generated from this same height function.
        // Reconcile the controller against it every frame so a missed mesh contact
        // can never let the hero fall below the walkable ground.
        SnapToGround(false);

        if (AshCombatButton.AttackPressed)
        {
            AshCombatButton.AttackPressed = false;
            var hits = Physics.OverlapSphere(transform.position + transform.forward * 1.35f + Vector3.up * 0.6f, 1.45f);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponentInParent<AshEnemy>();
                if (enemy != null)
                {
                    AshDarkFantasyPresentation.SpawnDarkSpell(hit.ClosestPoint(transform.position + transform.forward * 1.35f));
                    enemy.TakeDamage(25f);
                }
            }
        }
    }

    void SnapToGround(bool force)
    {
        float groundY = AshGroundSafety.SampleGroundHeight(transform.position);
        float halfHeight = cc.height * 0.5f;
        float targetY = groundY + halfHeight + GroundClearance;
        float footY = transform.position.y - halfHeight;

        // Snap on spawn, or when the feet touch/penetrate the generated terrain.
        if (force || footY < groundY + GroundClearance)
        {
            Vector3 position = transform.position;
            position.y = targetY;
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
            if (velocity.y < 0f) velocity.y = 0f;
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