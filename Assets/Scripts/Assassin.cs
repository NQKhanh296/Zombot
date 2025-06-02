using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Assassin : Enemy
{
    [Header("Attack")]
    protected int attackPhase = 0;
    protected float lastAttackTime = 0f;
    public float attackSpeed = 1f;
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

    /// <summary>
    /// Fixed update loop - handles dash initiation when not currently dashing
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Only try to dash when not already dashing
        if (!isDashing)
            TryStartDash();
    }

    /// <summary>
    /// Handles player attack logic with combo system
    /// Cycles through 3 different attack animations
    /// </summary>
    protected override void AttackPlayer()
    {
        // Don't attack if dashing or if attack is on cooldown
        if (isDashing || Time.time - lastAttackTime < attackSpeed) return;

        // Cycle through attack phases 1-3
        attackPhase = (attackPhase % 3) + 1;

        // Trigger the appropriate attack animation
        animator.SetTrigger($"Attack{attackPhase}");

        // Prevent movement during attack
        canMove = false;
        lastAttackTime = Time.time;
    }

    /// <summary>
    /// Handles death behavior - triggers death animation
    /// </summary>
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }

    /// <summary>
    /// Performs damage detection using a box cast from the attack point
    /// Applies damage, knockback, and status effects to hit players
    /// </summary>
    protected override void Damage()
    {
        // Determine attack direction based on facing direction
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        Vector2 size = new(damageWidth, damageHeight);
        Vector2 origin = (Vector2)attackPoint.position;

        // Cast a box to detect all colliders in the damage area
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, direction, 0.1f, playerLayer);

        // Apply damage to all hit players
        foreach (var hit in hits)
        {
            var player = hit.collider.GetComponentInParent<Character>();
            if (player != null)
            {
                // Apply damage with knockback effects
                player.TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
            }
        }
    }

    //================== DASH ==================//

    /// <summary>
    /// Attempts to initiate a dash if conditions are met
    /// Checks cooldown, distance to player, and current state
    /// </summary>
    private void TryStartDash()
    {
        // Exit early if dash is not available or player is not assigned
        if (!canDash || isDashing || current_player == null) return;

        // Check if dash cooldown has elapsed
        float timeSinceLastDash = Time.time - lastDashTime;
        if (timeSinceLastDash < dashCooldown) return;

        // Only dash if player is within follow range
        float distanceToPlayer = Vector2.Distance(transform.position, current_player.GetTransform().position);
        if (distanceToPlayer < followRange)
        {
            // Prepare for dash: disable flipping and movement
            disableFlip = true;
            canMove = false;

            // Trigger dash animation
            animator.SetTrigger("Dash");
            lastDashTime = Time.time;
        }
    }

    /// <summary>
    /// Animation event callback to start the dash coroutine
    /// Called from the dash animation
    /// </summary>
    public void TriggerDash()
    {
        if (!isDashing) StartCoroutine(Dash());
    }

    /// <summary>
    /// Coroutine that handles the dash movement mechanics
    /// Provides temporary immunity, disables gravity, and moves the assassin rapidly
    /// </summary>
    private IEnumerator Dash()
    {
        // Set dash state and grant temporary immunity
        isDashing = true;
        Immunity();

        // Store original gravity and disable it during dash
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        // Calculate dash parameters
        Vector2 dashDir = facingRight ? Vector2.right : Vector2.left;
        float dashDuration = dashDistance / dashSpeed;
        float elapsed = 0f;

        // Disable normal movement and attacks during dash
        currentSpeed = 0;
        canAttack = false;

        // Perform the dash movement
        while (elapsed < dashDuration)
        {
            float step = dashSpeed * Time.fixedDeltaTime;
            Vector2 nextPos = (Vector2)transform.position + dashDir * step;

            // Create a collision detection box slightly smaller than the assassin
            Vector2 boxSize = GetComponent<Collider2D>().bounds.size;
            boxSize.y = Mathf.Max(0.01f, boxSize.y - 1f); // Reduce height to avoid ground collision
            boxSize.x = 0.01f; // Thin box for forward collision detection

            // Check for obstacles in the dash path
            RaycastHit2D hit = Physics2D.BoxCast((Vector2)transform.position, boxSize, 0f, dashDir, step, collidableLayersWhenDash);
            if (hit.collider != null)
                break; // Stop dash if obstacle is hit

            // Move the rigidbody
            rb.MovePosition(rb.position + dashDir * step);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Restore normal physics and abilities
        rb.gravityScale = originalGravity;
        isDashing = false;
        disableFlip = false;
        canMove = true;
        canAttack = true;
        currentSpeed = moveSpeed;
        EndImmunity();

        // Wait for cooldown before allowing next dash
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    //================== GIZMOS ==================//

    /// <summary>
    /// Draws debug gizmos in the Scene view for visualization
    /// Shows attack range and dash distance
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        // Draw attack damage area
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, new Vector2(damageWidth, damageHeight));
        }

        // Draw dash distance indicator
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        // Note: Direction appears inverted in gizmo compared to actual dash direction
        Vector3 end = start + new Vector3((facingRight ? -1 : 1) * dashDistance, 0f, 0f);
        Gizmos.DrawLine(start, end);

        // Call base class gizmo drawing
        base.OnDrawGizmosSelected();
    }
}
