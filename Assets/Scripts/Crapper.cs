using Assets.Scripts;
using System.Collections;
using UnityEngine;

public class Crapper : Enemy
{
    public GameObject explosionPrefab;
    public float delayBeforeExplosion = 0.5f;
    public float explosionDuration = 0.5f;

    protected override void AttackPlayer()
    {
        Die();
    }
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }
    private void InstantiateExplosion()
    {
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(explosion, explosionDuration);
        base.DestroyObject();
    }
}