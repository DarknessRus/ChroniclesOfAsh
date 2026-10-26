using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class AshPlayerController : MonoBehaviour {
 public float moveSpeed=4.2f, gravity=-18f;
 CharacterController cc; Vector3 velocity;
 void Awake(){cc=GetComponent<CharacterController>();}
 void Update(){
  Vector2 touch=AshVirtualJoystick.InputVector;
  float h=Mathf.Abs(touch.x)>.08f?touch.x:Input.GetAxisRaw("Horizontal");
  float v=Mathf.Abs(touch.y)>.08f?touch.y:Input.GetAxisRaw("Vertical");
  Vector3 input=new Vector3(h,0,v); if(input.sqrMagnitude>1)input.Normalize();
  Vector3 move=CameraRelative(input);
  if(move.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(move),12f*Time.deltaTime);
  cc.Move(move*moveSpeed*Time.deltaTime);
  if(cc.isGrounded&&velocity.y<0)velocity.y=-2f;
  velocity.y+=gravity*Time.deltaTime; cc.Move(velocity*Time.deltaTime);
  if(AshCombatButton.AttackPressed){AshCombatButton.AttackPressed=false;var hits=Physics.OverlapSphere(transform.position+transform.forward*1.35f+Vector3.up*.6f,1.45f);foreach(var hit in hits){var enemy=hit.GetComponentInParent<AshEnemy>();if(enemy!=null)enemy.TakeDamage(25f);}}
 }
 Vector3 CameraRelative(Vector3 input){
  var cam=Camera.main;if(cam==null)return input;
  Vector3 f=cam.transform.forward;f.y=0;f.Normalize();
  Vector3 r=cam.transform.right;r.y=0;r.Normalize();
  return (f*input.z+r*input.x).normalized;
 }
}