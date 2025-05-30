using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordMaster : Character
{
    #region Attack System
    [Header("Combat Settings")]
    [Tooltip("Current phase in the 4-hit combo sequence (1-4, cycles back to 1)")]
    private int attackPhase = 0;

    [Tooltip("Timestamp of the last attack execution for cooldown management")]
    private float lastAttackTime = 0f;

    [Tooltip("Time interval between attacks in seconds")]
    public float attackSpeed = 0.5f;

    [Tooltip("Transform point where damage detection originates")]
    public Transform attackPoint;

    [Tooltip("Width of the melee damage detection box")]
    public float damageWidth = 0.5f;

    [Tooltip("Height of the melee damage detection box")]
    public float damageHeight = 0.5f;

    [Tooltip("Layer mask for enemy detection during attacks")]
    public LayerMask enemyLayer;

    [Tooltip("Layer mask for collision detection during dash (walls, obstacles)")]
    public LayerMask collidableLayers;

    [Tooltip("Base damage dealt per attack")]
    public int attackDamage = 40;
    #endregion

    #region Dash System
    [Header("Dash Ability")]
    [Tooltip("Whether the dash ability is currently available")]
    private bool canDash = true;

    [Tooltip("Whether the character is currently performing a dash")]
    private bool isDashing;

    [Tooltip("Distance covered during a single dash")]
    public float dashDistance = 10f;

    [Tooltip("Speed of dash movement")]
    public float dashSpeed = 10f;

    [Tooltip("Cooldown time before dash can be used again")]
    public float dashCooldown = 1f;

    [Tooltip("UI bar displaying dash availability/cooldown")]
    public StatusBar dashBar;

    [Tooltip("Maximum value for the dash bar (typically 1 for full availability)")]
    public int dashbarMaxValue = 1;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initialize SwordMaster-specific components and UI elements
    /// </summary>
    protected override void Awake()
    {
        // Call parent initialization
        base.Awake();

        // Set up dash UI bar
        dashBar.gameObject.SetActive(true);
        dashBar.SetMaxValue(dashbarMaxValue);
        dashBar.SetValue(dashbarMaxValue); // Start with dash available
    }

    /// <summary>
    /// Main update loop with SwordMaster-specific behavior
    /// Handles dash input and updates animation states
    /// </summary>
    protected override void FixedUpdate()
    {
        // Execute base character behavior (movement, jumping, etc.)
        base.FixedUpdate();

        // Handle dash input detection
        TriggerDash();

        // Update animation parameters for combat and movement states
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsDashing", isDashing);
    }
    #endregion

    #region Combat System
    /// <summary>
    /// Handles sword attack input and combo system.
    /// Implements a 4-phase combo that cycles (1->2->3->4->1).
    /// Each attack phase triggers different animations and can have unique properties.
    /// </summary>
    protected override void Attack()
    {
        // Don't attack if controls are disabled (during knockback, dash, etc.)
        if (controlDisabled)
        {
            return;
        }

        // Check for attack input (J key) and cooldown
        if (Input.GetKey(KeyCode.J) && (Time.time - lastAttackTime > attackSpeed))
        {
            // Set attacking state for animation and logic
            isAttacking = true;

            // Cycle through attack phases (1->2->3->4->1)
            attackPhase = (attackPhase % 4) + 1;

            // Trigger the appropriate attack animation
            animator.SetTrigger($"Attack{attackPhase}");

            // Record attack time for cooldown calculation
            lastAttackTime = Time.time;
        }
    }

    /// <summary>
    /// Resets the attacking state, typically called at the end of attack animations.
    /// This allows the character to move and perform other actions again.
    /// Called via Animation Events.
    /// </summary>
    private void ResetAttack()
    {
        isAttacking = false;
    }

    /// <summary>
    /// Executes damage detection and application for sword attacks.
    /// Uses box casting to detect enemies in front of the character.
    /// Called during attack animation events at the moment of impact.
    /// </summary>
    protected void Damage()
    {
        // Determine attack direction based on character facing
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;

        // Define damage detection box size
        Vector2 size = new(damageWidth, damageHeight);

        // Set origin point for damage detection
        Vector2 origin = (Vector2)attackPoint.position;

        // Perform box cast to detect all enemies in attack range
        // Small distance (0.1f) creates a thick detection zone
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, direction, 0.1f, enemyLayer);

        // Process each enemy hit
        foreach (var hit in hits)
        {
            var enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Apply damage with knockback effects
                // Parameters: damage, source, knockbackX, knockbackY, duration
                enemy.TakeDamage(attackDamage, transform, 10f, 10f, 0.6f);
            }
        }
    }
    #endregion

    #region Dash System Implementation
    /// <summary>
    /// Detects dash input and initiates dash sequence if available.
    /// Checks for Left Shift key input and dash availability.
    /// </summary>
    private void TriggerDash()
    {
        // Don't dash if controls are disabled
        if (controlDisabled)
        {
            return;
        }

        // Check for dash input (Left Shift) and availability
        if (Input.GetKey(KeyCode.LeftShift) && canDash)
        {
            // Trigger dash animation
            animator.SetTrigger("Dash");

            // Start dash coroutine
            StartCoroutine(Dash());
        }
    }

    /// <summary>
    /// Coroutine that handles the complete dash sequence:
    /// 1. Movement with collision detection
    /// 2. Temporary invincibility
    /// 3. Cooldown management with UI feedback
    /// 4. Dash availability restoration
    /// </summary>
    private IEnumerator Dash()
    {
        // === DASH INITIALIZATION ===
        canDash = false;           // Prevent multiple dashes
        isDashing = true;          // Set dashing state
        controlDisabled = true;    // Disable player input
        rb.velocity = Vector2.zero; // Reset current velocity
        dashBar.SetValue(0);       // Update UI to show dash used

        // Store original physics settings
        float originalGravity = rb.gravityScale;

        // Enable temporary invincibility and disable gravity
        Immunity();
        rb.gravityScale = 0f;

        // Calculate dash parameters
        Vector2 dashDir = facingRight ? Vector2.right : Vector2.left;
        float dashDuration = dashDistance / dashSpeed;
        float elapsed = 0f;

        // Disable normal movement speed during dash
        currentSpeed = 0;

        // === DASH MOVEMENT LOOP ===
        while (elapsed < dashDuration)
        {
            // Calculate next movement step
            float step = dashSpeed * Time.fixedDeltaTime;
            Vector2 nextPos = rb.position + dashDir * step;

            // Set up collision detection box (thin vertical slice for wall detection)
            Vector2 boxSize = GetComponent<Collider2D>().bounds.size;
            boxSize.y = Mathf.Max(0.01f, boxSize.y - 1f); // Reduce height to avoid ground collision
            boxSize.x = 0.01f; // Very thin for precise wall detection

            // Check for collision in dash direction
            RaycastHit2D hit = Physics2D.BoxCast(rb.position, boxSize, 0f, dashDir, step, collidableLayers);
            if (hit.collider != null)
            {
                // Stop dash if collision detected (hit a wall)
                break;
            }

            // Move to next position and update timer
            rb.MovePosition(nextPos);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // === DASH CLEANUP ===
        // Restore physics settings
        rb.gravityScale = originalGravity;
        isDashing = false;
        controlDisabled = false;

        // Brief pause before restoring movement
        yield return new WaitForSeconds(0.1f);
        currentSpeed = moveSpeed;
        EndImmunity();

        // === COOLDOWN AND UI UPDATE ===
        float refillElapsed = 0f;

        // Gradually fill dash bar during cooldown
        while (refillElapsed < dashCooldown)
        {
            float t = refillElapsed / dashCooldown;  // Calculate progress (0 to 1)
            dashBar.SetValue(t);                     // Update UI bar
            refillElapsed += Time.deltaTime;         // Increment timer
            yield return null;                       // Wait one frame
        }

        // Ensure bar is fully filled and dash is available
        dashBar.SetValue(1);
        canDash = true;
    }
    #endregion

    #region Invincibility System
    /// <summary>
    /// Grants temporary invincibility by changing the character's layer.
    /// Used during dash to prevent damage from enemies.
    /// </summary>
    private void Immunity()
    {
        gameObject.layer = LayerMask.NameToLayer("Invincible");
    }

    /// <summary>
    /// Removes invincibility by restoring the character's normal layer.
    /// Called after dash completion to restore normal damage interactions.
    /// </summary>
    private void EndImmunity()
    {
        gameObject.layer = LayerMask.NameToLayer("Player");
    }
    #endregion

    #region Death Handling
    /// <summary>
    /// Handles SwordMaster death by triggering the death animation.
    /// Overrides the base Character die method.
    /// </summary>
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }
    #endregion

    #region Debug Visualization
    /// <summary>
    /// Draws debug gizmos in the Scene view for visual debugging.
    /// Shows attack damage box and dash distance/direction.
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        // Don't draw if attack point is not set
        if (attackPoint == null) return;

        // Draw attack damage detection box in red
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, new Vector2(damageWidth, damageHeight));

        // Draw dash distance and direction in cyan
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + new Vector3((facingRight ? 1 : -1) * dashDistance, 0f, 0f);
        Gizmos.DrawLine(start, end);

        // Draw base character gizmos (ground check, etc.)
        base.OnDrawGizmosSelected();
    }
    #endregion

}
