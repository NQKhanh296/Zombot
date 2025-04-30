using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Shooter : Character
{
    public Transform firePoint;
    public GameObject bullet;
    public float fireRate = 0.5f;
    private float nextFireTime = 0f;
    public AudioSource shoot_sfx;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        Respawn();
    }
    protected override void Attack()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            animator.SetTrigger("Shoot");
            Instantiate(bullet, firePoint.position, firePoint.rotation);
            shoot_sfx.Play();
            nextFireTime = Time.time + fireRate;
        }
    }
    private void Respawn()
    {
        if (transform.position.y < -6)
        {
            transform.position = new Vector2(0, 0);
        }
    }
}
    