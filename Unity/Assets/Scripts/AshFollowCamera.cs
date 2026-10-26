using UnityEngine;
public class AshFollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset=new Vector3(0,5.5f,-8f);
    void LateUpdate()
    {
        if(target==null)return;
        transform.position=Vector3.Lerp(transform.position,target.position+offset,Time.deltaTime*5f);
        transform.LookAt(target.position+Vector3.up*1.1f);
    }
}
