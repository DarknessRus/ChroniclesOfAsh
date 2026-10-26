using UnityEngine;
public class AshEnemy : MonoBehaviour
{
 public float health=60f;
 public void TakeDamage(float amount)
 {
  health-=amount;
  var renderer=GetComponentInChildren<Renderer>();
  if(renderer!=null)renderer.material.color=Color.Lerp(Color.black,new Color(.75f,.12f,.08f),Mathf.Clamp01(health/60f));
  if(health<=0)Destroy(gameObject);
 }
}