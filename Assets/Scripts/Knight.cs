using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : Character
{
    private int attackPhase = 0;
    private float lastAttackTime = 0f;
    public float attackSpeed = 0.5f;
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayer;
    public int attackDamage = 40;


    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        Respawn();
    }

    private void Respawn()
    {
        if (transform.position.y < -6)
        {
            transform.position = new Vector2(0, 0);
        }
    }

    protected override void Attack()
    {
        if (Input.GetButton("Fire1") && (Time.time - lastAttackTime > attackSpeed) && !controlDisabled)
        {
            attackPhase = (attackPhase % 3) + 1;
            animator.SetTrigger($"Attack{attackPhase}");
            lastAttackTime = Time.time;
        }
    }

    protected void Damage()
    {
        float radius = attackRange;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        float castDistance = 0.1f;
        Vector2 origin = (Vector2)attackPoint.position;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, radius, direction, castDistance, enemyLayer);

        foreach (var hit in hits)
        {
            var enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage, transform, 10f, 0.6f);
            }
        }
    }
    protected override void Die()   
    {
        animator.SetTrigger("Die");
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        float radius = attackRange;
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)attackPoint.position + direction * 0.05f;

        Gizmos.DrawWireSphere(origin, radius);
    }

}
