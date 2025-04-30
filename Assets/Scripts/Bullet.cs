using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletSpeed = 30f;
    public int bulletDamage = 20;
    public Rigidbody2D rb;
    public GameObject explosionPrefab;
    private float explosion_duration = 0.2f;
    void Start()
    {
        rb.velocity = transform.right * bulletSpeed;
        Destroy(gameObject, 3f);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.isTrigger)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, explosion_duration);
            Destroy(gameObject);
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(bulletDamage, transform,10f,0.3f);
            }
        }
    }
}
