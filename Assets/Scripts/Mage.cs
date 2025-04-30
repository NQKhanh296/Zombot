using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mage : Enemy
{
    [Header("Attack")]
    protected int attackPhase = 0;
    protected float lastAttackTime = 0f;
    public float attackSpeed = 0.5f;
    public float damageWidth = 0.5f;
    public float damageHeight = 0.5f;
    public float attackPrecision = 0.5f;
    public int healingAmount = 20;
    public GameObject attackVfx;
    protected override void Awake()
    {
        base.Awake();
        attackVfx.GetComponent<MageAttack>().damage = attackDamage;
    }
    protected override void AttackPlayer()
    {
        if (Time.time - lastAttackTime < attackSpeed) return;

        float chance = Random.Range(0f, 1f);  // Random value between 0 and 1

        int randomPhase;
        if (chance <= 0.7f)  // 70% chance
        {
            randomPhase = 1;
        }
        else  // 30% chance
        {
            randomPhase = 2;
        }

        animator.SetTrigger($"Attack{randomPhase}");

        canMove = false;
        lastAttackTime = Time.time;
    }
    protected override void Damage()
    {
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        Vector2 size = new(damageWidth, damageHeight);
        Vector2 origin = (Vector2)attackPoint.position;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, direction, 0.1f, playerLayer);

        foreach (var hit in hits)
        {
            var player = hit.collider.GetComponentInParent<Character>();
            if (player != null)
            {
                player.TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
            }
        }
    }
    private float GetAttackVfxHeight()
    {
        if (attackVfx == null) return 1f;

        BoxCollider2D box = attackVfx.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            return box.size.y * attackVfx.transform.localScale.y;
        }

        return 1f; 
    }
    private void SummonAttack()
    {
        var hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);

        float playerX = hitPlayer != null ? hitPlayer.transform.position.x : transform.position.x;
        float randomX = Random.Range(playerX - attackPrecision, playerX + attackPrecision);

        // Raycast down to find the ground
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(randomX, transform.position.y), Vector2.down, Mathf.Infinity, groundLayer);

        // If the raycast hits something (ground), use that position for Y
        float groundY = hit.collider != null ? hit.point.y : transform.position.y; // Default to the enemy position if no ground found

        // Set the final position for the attack VFX
        Vector2 spawnPosition = new Vector2(randomX, groundY + GetAttackVfxHeight());

        // Instantiate the attack VFX at the calculated position
        Instantiate(attackVfx, spawnPosition, Quaternion.identity);
    }
    private void Heal()
    {
        if (currentHp >= maxHP || currentHp == 0) return;
        if (currentHp + healingAmount > maxHP)
        {
            currentHp = maxHP;
        }
        else currentHp += healingAmount;
    }
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, new Vector2(damageWidth, damageHeight));
        }
    }
}
