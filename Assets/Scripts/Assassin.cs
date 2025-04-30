using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Assassin : Enemy
{
    [Header("Attack")]
    protected int attackPhase = 0;
    protected float lastAttackTime = 0f;
    public float attackSpeed = 0.5f;
    public float damageWidth = 0.5f;
    public float damageHeight = 0.5f;

    [Header("Dash")]
    public float dashDistance = 10f;
    public float dashSpeed = 10f;
    public float dashCooldown = 3f;
    public LayerMask collidableLayersWhenDash;

    protected bool canDash = true;
    protected bool isDashing = false;
    protected float lastDashTime = Mathf.NegativeInfinity;

    //================== MAIN LOGIC ==================//
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!isDashing)
            TryStartDash();
    }

    protected override void AttackPlayer()
    {
        if (isDashing || Time.time - lastAttackTime < attackSpeed) return;

        attackPhase = (attackPhase % 3) + 1;
        animator.SetTrigger($"Attack{attackPhase}");
        canMove = false;
        lastAttackTime = Time.time;
    }


    protected override void Die()
    {
        animator.SetTrigger("Die");
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

    //================== DASH ==================//
    private void TryStartDash()
    {
        if (!canDash || isDashing || current_player == null) return;

        float timeSinceLastDash = Time.time - lastDashTime;
        if (timeSinceLastDash < dashCooldown) return;

        float distanceToPlayer = Vector2.Distance(transform.position, current_player.GetTransform().position);
        if (distanceToPlayer < followRange)
        {
            disableFlip = true;
            canMove = false;
            animator.SetTrigger("Dash");
            lastDashTime = Time.time;
        }
    }
    public void TriggerDash()
    {
        if (!isDashing) StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        Immunity();

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        Vector2 dashDir = facingRight ? Vector2.right : Vector2.left;
        float dashDuration = dashDistance / dashSpeed;
        float elapsed = 0f;

        currentSpeed = 0;
        canAttack = false;

        while (elapsed < dashDuration)
        {
            float step = dashSpeed * Time.fixedDeltaTime;
            Vector2 nextPos = (Vector2)transform.position + dashDir * step;

            Vector2 boxSize = GetComponent<Collider2D>().bounds.size;
            boxSize.y = Mathf.Max(0.01f, boxSize.y - 1f);
            boxSize.x = 0.01f;

            RaycastHit2D hit = Physics2D.BoxCast((Vector2)transform.position, boxSize, 0f, dashDir, step, collidableLayersWhenDash);
            if (hit.collider != null)
                break;

            rb.MovePosition(rb.position + dashDir * step);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.gravityScale = originalGravity;
        isDashing = false;
        disableFlip = false;
        canMove = true;
        canAttack = true;
        currentSpeed = moveSpeed;
        EndImmunity();

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    //================== GIZMOS ==================//
    protected override void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, new Vector2(damageWidth, damageHeight));
        }

        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + new Vector3((facingRight ? -1 : 1) * dashDistance, 0f, 0f);
        Gizmos.DrawLine(start, end);

        base.OnDrawGizmosSelected();
    }
}
