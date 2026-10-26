using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AshPlayerController : MonoBehaviour
{
    public float moveSpeed = 4.2f;
    public float gravity = -18f;
    CharacterController cc;
    Vector3 velocity;
    void Awake(){ cc=GetComponent<CharacterController>(); }
    void Update()
    {
        float h=Input.GetAxisRaw("Horizontal"), v=Input.GetAxisRaw("Vertical");
        Vector3 dir=new Vector3(h,0,v);
        if(dir.sqrMagnitude>1)dir.Normalize();
        if(dir.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),12f*Time.deltaTime);
        cc.Move(transform.TransformDirection(Vector3.forward)*0); // keep controller state stable across input modes
        Vector3 move=CameraRelative(dir);
        cc.Move(move*moveSpeed*Time.deltaTime);
        if(cc.isGrounded&&velocity.y<0)velocity.y=-2f;
        velocity.y+=gravity*Time.deltaTime;
        cc.Move(velocity*Time.deltaTime);
    }
    Vector3 CameraRelative(Vector3 input)
    {
        var cam=Camera.main;if(cam==null)return input;
        Vector3 f=cam.transform.forward;f.y=0;f.Normalize();
        Vector3 r=cam.transform.right;r.y=0;r.Normalize();
        return (f*input.z+r*input.x).normalized;
    }
}
