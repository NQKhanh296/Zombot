using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mage : Enemy
{
    #region Attack Configuration
    [Header("Attack Settings")]
    [Tooltip("Current phase of the attack sequence (0 = idle, 1 = primary attack, 2 = secondary attack)")]
    protected int attackPhase = 0;

    [Tooltip("Timestamp of the last attack execution")]
    protected float lastAttackTime = 0f;

    [Tooltip("Time interval between attacks in seconds")]
    public float attackSpeed = 1f;

    [Tooltip("Width of the melee damage detection box")]
    public float damageWidth = 0.5f;

    [Tooltip("Height of the melee damage detection box")]
    public float damageHeight = 0.5f;

    [Tooltip("Maximum random offset for attack positioning (higher = less precise)")]
    public float attackPrecision = 0.5f;

    [Tooltip("Amount of health restored when using healing ability")]
    public int healingAmount = 20;

    [Tooltip("Visual effect prefab spawned during magical attacks")]
    public GameObject attackVfx;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initialize mage-specific components and configure attack VFX damage
    /// </summary>
    protected override void Awake()
    {
        // Call parent initialization first
        base.Awake();

        // Configure the attack VFX damage to match this mage's attack damage
        attackVfx.GetComponent<MageAttack>().damage = attackDamage;
    }
    #endregion

    #region Attack System
    /// <summary>
    /// Handles the mage's attack logic with probabilistic phase selection.
    /// 70% chance for Phase 1 attack, 30% chance for Phase 2 attack.
    /// Includes attack cooldown management and movement restriction during attacks.
    /// </summary>
    protected override void AttackPlayer()
    {
        // Enforce attack cooldown - prevent attacks if not enough time has passed
        if (Time.time - lastAttackTime < attackSpeed) return;

        // Generate random value for attack phase selection
        float chance = Random.Range(0f, 1f);  // Random value between 0 and 1
        int randomPhase;

        // Weighted random selection for attack phases
        if (chance <= 0.7f)  // 70% chance for primary attack
        {
            randomPhase = 1;
        }
        else  // 30% chance for secondary attack (healing or special ability)
        {
            randomPhase = 2;
        }

        // Trigger the appropriate attack animation
        animator.SetTrigger($"Attack{randomPhase}");

        // Disable movement during attack execution
        canMove = false;

        // Record the time of this attack for cooldown calculation
        lastAttackTime = Time.time;
    }

    /// <summary>
    /// Executes melee damage detection using box casting.
    /// Creates a damage box in front of the mage and applies damage to any players within range.
    /// Called during attack animation events.
    /// </summary>
    protected override void Damage()
    {
        // Determine attack direction based on which way the mage is facing
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;

        // Define the size of the damage detection box
        Vector2 size = new(damageWidth, damageHeight);

        // Set the origin point for damage detection (attack point transform)
        Vector2 origin = (Vector2)attackPoint.position;

        // Perform box cast to detect all colliders in the damage area
        // Small distance (0.1f) to create a thick detection zone
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, direction, 0.1f, playerLayer);

        // Process each hit target
        foreach (var hit in hits)
        {
            // Attempt to get the Character component from the hit object or its parent
            var player = hit.collider.GetComponentInParent<Character>();
            if (player != null)
            {
                // Apply damage with knockback effects
                player.TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
            }
        }
    }
    #endregion

    #region Magical Abilities
    /// <summary>
    /// Calculates the height of the attack VFX based on its collider size and scale.
    /// Used to properly position ground-based magical effects.
    /// </summary>
    /// <returns>The calculated height of the attack VFX, or 1f as default</returns>
    private float GetAttackVfxHeight()
    {
        // Safety check - return default height if VFX is not assigned
        if (attackVfx == null) return 1f;

        // Get the BoxCollider2D component from the attack VFX
        BoxCollider2D box = attackVfx.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            // Calculate actual height considering both collider size and transform scale
            return box.size.y * attackVfx.transform.localScale.y;
        }

        // Return default height if no collider is found
        return 1f;
    }

    /// <summary>
    /// Summons a magical attack at the player's approximate location.
    /// Uses ground detection to properly position the attack VFX on the terrain.
    /// Includes positioning randomization based on attackPrecision for gameplay variety.
    /// Called during attack animation events.
    /// </summary>
    private void SummonAttack()
    {
        // Detect if player is within attack range
        var hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);

        // Determine target X position (player's position if detected, otherwise mage's position)
        float playerX = hitPlayer != null ? hitPlayer.transform.position.x : transform.position.x;

        // Add random offset to make attacks less predictable
        float randomX = Random.Range(playerX - attackPrecision, playerX + attackPrecision);

        // Cast ray downward from the target X position to find ground level
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(randomX, transform.position.y), Vector2.down, Mathf.Infinity, groundLayer);

        // Calculate ground Y position (use hit point if ground found, otherwise use mage's Y position)
        float groundY = hit.collider != null ? hit.point.y : transform.position.y;

        // Calculate final spawn position, placing VFX slightly above ground based on its height
        Vector2 spawnPosition = new Vector2(randomX, groundY + GetAttackVfxHeight());

        // Instantiate the magical attack VFX at the calculated position
        Instantiate(attackVfx, spawnPosition, Quaternion.identity);
    }

    /// <summary>
    /// Restores health to the mage up to maximum HP.
    /// Includes safety checks to prevent healing when dead or already at full health.
    /// Called during healing animation events or as part of Attack Phase 2.
    /// </summary>
    private void Heal()
    {
        // Don't heal if already at max health or if dead
        if (currentHp >= maxHP || currentHp == 0) return;

        // Cap healing to maximum HP to prevent over-healing
        if (currentHp + healingAmount > maxHP)
        {
            currentHp = maxHP;
        }
        else
        {
            currentHp += healingAmount;
        }
    }
    #endregion

    #region Death Handling
    /// <summary>
    /// Handles mage death by triggering the death animation.
    /// Overrides the base Enemy die method to provide mage-specific death behavior.
    /// </summary>
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }
    #endregion

    #region Debug Visualization
    /// <summary>
    /// Draws debug gizmos in the Scene view to visualize attack ranges and damage areas.
    /// Extends the base enemy gizmos with mage-specific melee damage box visualization.
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        // Draw base enemy gizmos (attack range, detection range, etc.)
        base.OnDrawGizmosSelected();

        // Draw melee damage detection box
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, new Vector2(damageWidth, damageHeight));
        }
    }
    #endregion
}
