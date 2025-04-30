using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlyingEye : Enemy
{
    private int attackPhase = 0;
    private float lastAttackTime = 0f;
    public float attackSpeed = 0.5f;
    public Transform attackPoint1;
    public Transform attackPoint2;
    public float attackRange1 = 0.5f;
    public float attackRange2 = 0.5f;
    private float currAttackRange1;
    private float currAttackRange2;
    public float flyHeight = 1.5f; 
    private float currentFlyHeight;
    private int currAttackPoint;
    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0;
        currAttackPoint = 1;
        currAttackRange1 = attackRange1;
        currAttackRange2 = attackRange2;
        currentFlyHeight = flyHeight;
    }
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }
    protected override void InAttackRange()
    {
        var hitPlayers = new List<Collider2D>();
        if (attackPoint1 == null || attackPoint2 == null)
        {
            return;
        }
        if (currAttackPoint == 1)
        {
            hitPlayers.Clear();
            hitPlayers = Physics2D.OverlapCircleAll(attackPoint1.position, currAttackRange1, playerLayer).ToList();
        }
        else if (currAttackPoint == 2)
        {
            hitPlayers.Clear();
            hitPlayers = Physics2D.OverlapCircleAll(attackPoint2.position, currAttackRange2, playerLayer).ToList();
        }
        if (hitPlayers.Any())
        {
            canMove = false;
            if (canAttack) AttackPlayer();
        }
    }
    protected override void AttackPlayer()
    {
        if (Time.time - lastAttackTime > attackSpeed)
        {
            attackPhase = (attackPhase % 2) + 1;
            animator.SetTrigger($"Attack{attackPhase}");
            currAttackPoint = attackPhase;
            lastAttackTime = Time.time;
        }
    }
    private void MaintainFlyHeight()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, groundLayer);
        if (hit.collider != null)
        {

            float groundY = hit.point.y;
            float desiredY = groundY + currentFlyHeight;
            transform.position = new Vector2(transform.position.x, desiredY);
        }
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        MaintainFlyHeight();
    }
    protected void DamageAttack1()
    {
        var hitPlayers = Physics2D.OverlapCircleAll(attackPoint1.position, attackRange1, playerLayer);
        foreach (var p in hitPlayers)
        {
            p.GetComponent<Character>().TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
        }
    }
    protected void DamageAttack2()
    {
        var hitPlayers = Physics2D.OverlapCircleAll(attackPoint2.position, attackRange2, playerLayer);
        foreach (var p in hitPlayers)
        {
            p.GetComponent<Character>().TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
        }
    }
    protected override IEnumerator ResetKnockbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canAttack = true;
        canMove = true;
        isKnockedBack = false;
        currentSpeed = moveSpeed;
        rb.velocity = Vector2.zero;
    }
    protected override void CheckAndJumpOverObstacle()
    {
        if (!canMove || !current_player || isKnockedBack) return;

        // Determine ray direction based on facing
        Vector2 rayDir = facingRight ? Vector2.right : Vector2.left;

        // Cast a short horizontal ray from the check point
        RaycastHit2D hit = Physics2D.Raycast(
            obstacleCheckPoint.position,
            rayDir,
            obstacleCheckDistance,
            groundLayer
        );

        // Jump if obstacle detected AND enemy is grounded
        if (hit.collider != null)
        {
            currentFlyHeight = 3f;
            StartCoroutine(ResetFlyHeight());
        }
    } 
    
    private IEnumerator ResetFlyHeight()
    {
        yield return new WaitForSeconds(1f);
        currentFlyHeight = flyHeight;
    }

    protected override void OnDrawGizmosSelected()
    {
        if (attackPoint1 == null || attackPoint2 == null)
        {
            return;
        }
        Gizmos.DrawWireSphere(attackPoint1.position, attackRange1);
        Gizmos.DrawWireSphere(attackPoint2.position, attackRange2);
        base.OnDrawGizmosSelected();
    }
    protected override void Immunity()
    {
        base.Immunity();
        flyHeight = 0;
        rb.gravityScale = 6;
    }
}
