using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public int damage = 20;
    public float explosion_radius = 1f;
    public LayerMask damageableLayer;
    public void Explode()
    {
        var damageables = Physics2D.OverlapCircleAll(transform.position, explosion_radius, damageableLayer);
        foreach (var d in damageables)
        {
            d.GetComponent<IDamageable>().TakeDamage(damage,transform,10f,10f, 0.6f);
        }
        Destroy(gameObject); 
    }
    private void OnDrawGizmosSelected()
    { 
        Gizmos.DrawWireSphere(transform.position, explosion_radius);
    }
}
