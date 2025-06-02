using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlyingEye : Enemy
{
    private int attackPhase = 0;
    private float lastAttackTime = 0f;
    public float attackSpeed = 1f;
    public Transform attackPoint1;
    public Transform attackPoint2;
    public float attackRange1 = 0.5f;
    public float attackRange2 = 0.5f;
    private float currAttackRange1;
    private float currAttackRange2;
    public float flyHeight = 1.5f;
    private float currentFlyHeight;
    private int currAttackPoint;

    /// <summary>
    /// Initialize FlyingEye-specific properties
    /// Disables gravity, sets up attack points, and initializes flying behavior
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        // Disable gravity for flying behavior
        rb.gravityScale = 0;

        // Initialize attack system
        currAttackPoint = 1;
        currAttackRange1 = attackRange1;
        currAttackRange2 = attackRange2;
        currentFlyHeight = flyHeight;
    }

    /// <summary>
    /// Handles death behavior - triggers death animation
    /// </summary>
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }

    /// <summary>
    /// Custom attack range detection for dual attack points
    /// Checks the currently active attack point for player presence
    /// </summary>
    protected override void InAttackRange()
    {
        var hitPlayers = new List<Collider2D>();

        // Ensure both attack points are assigned
        if (attackPoint1 == null || attackPoint2 == null)
        {
            return;
        }

        // Check the currently active attack point for players
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

        // Initiate attack if players are detected
        if (hitPlayers.Any())
        {
            canMove = false;
            if (canAttack) AttackPlayer();
        }
    }

    /// <summary>
    /// Alternates between two different attack patterns
    /// Cycles attack phase and updates current attack point
    /// </summary>
    protected override void AttackPlayer()
    {
        // Check attack cooldown
        if (Time.time - lastAttackTime > attackSpeed)
        {
            // Alternate between attack phases 1 and 2
            attackPhase = (attackPhase % 2) + 1;

            // Trigger corresponding attack animation
            animator.SetTrigger($"Attack{attackPhase}");

            // Update current attack point to match attack phase
            currAttackPoint = attackPhase;
            lastAttackTime = Time.time;
        }
    }

    /// <summary>
    /// Maintains consistent flying height above the ground
    /// Uses raycast to detect ground and adjusts Y position accordingly
    /// </summary>
    private void MaintainFlyHeight()
    {
        // Cast ray downward to find ground level
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, groundLayer);

        if (hit.collider != null)
        {
            // Calculate desired position based on ground height
            float groundY = hit.point.y;
            float desiredY = groundY + currentFlyHeight;

            // Set position to maintain fly height
            transform.position = new Vector2(transform.position.x, desiredY);
        }
    }

    /// <summary>
    /// Extended FixedUpdate that includes fly height maintenance
    /// Calls base enemy behavior then maintains flying position
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        MaintainFlyHeight();
    }

    /// <summary>
    /// Animation event callback for first attack point damage application
    /// Detects and damages players within attack point 1's range
    /// </summary>
    protected void DamageAttack1()
    {
        var hitPlayers = Physics2D.OverlapCircleAll(attackPoint1.position, attackRange1, playerLayer);

        foreach (var p in hitPlayers)
        {
            p.GetComponent<Character>().TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
        }
    }

    /// <summary>
    /// Animation event callback for second attack point damage application
    /// Detects and damages players within attack point 2's range
    /// </summary>
    protected void DamageAttack2()
    {
        var hitPlayers = Physics2D.OverlapCircleAll(attackPoint2.position, attackRange2, playerLayer);

        foreach (var p in hitPlayers)
        {
            p.GetComponent<Character>().TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
        }
    }

    /// <summary>
    /// Custom knockback recovery that ensures flying enemy stops moving after knockback
    /// Overrides base method to reset velocity for flying behavior
    /// </summary>
    protected override IEnumerator ResetKnockbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canAttack = true;
        canMove = true;
        isKnockedBack = false;
        currentSpeed = moveSpeed;

        // Reset velocity to prevent continued movement after knockback
        rb.velocity = Vector2.zero;
    }

    /// <summary>
    /// Obstacle avoidance for flying enemy - temporarily increases fly height
    /// Instead of jumping, the flying eye rises higher to clear obstacles
    /// </summary>
    protected override void CheckAndJumpOverObstacle()
    {
        if (!canMove || !current_player || isKnockedBack) return;

        // Cast ray in facing direction to detect obstacles
        Vector2 rayDir = facingRight ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            obstacleCheckPoint.position,
            rayDir,
            obstacleCheckDistance,
            groundLayer
        );

        // Increase fly height when obstacle is detected
        if (hit.collider != null)
        {
            currentFlyHeight = 3f; // Temporarily fly higher
            StartCoroutine(ResetFlyHeight());
        }
    }

    /// <summary>
    /// Resets fly height back to normal after obstacle avoidance
    /// Coroutine that restores default flying height after a delay
    /// </summary>
    private IEnumerator ResetFlyHeight()
    {
        yield return new WaitForSeconds(1f);
        currentFlyHeight = flyHeight; // Return to normal fly height
    }

    /// <summary>
    /// Custom gizmo drawing for dual attack points
    /// Shows both attack ranges in the Scene view for debugging
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        // Ensure attack points are assigned before drawing
        if (attackPoint1 == null || attackPoint2 == null)
        {
            return;
        }

        // Draw attack ranges for both attack points
        Gizmos.DrawWireSphere(attackPoint1.position, attackRange1);
        Gizmos.DrawWireSphere(attackPoint2.position, attackRange2);

        // Draw base enemy gizmos (follow range, etc.)
        base.OnDrawGizmosSelected();
    }

    /// <summary>
    /// Custom immunity behavior for flying enemy
    /// When taking damage, temporarily becomes grounded with gravity
    /// </summary>
    protected override void Immunity()
    {
        base.Immunity();

        // Temporarily ground the flying enemy during immunity
        flyHeight = 0; // Set to ground level
        rb.gravityScale = 6; // Enable gravity to make it fall
    }
}
