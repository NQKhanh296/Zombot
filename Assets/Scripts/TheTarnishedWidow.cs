using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using Cinemachine;
using UnityEngine.SceneManagement;
using System;

public class TheTarnishedWidow : Enemy
{
    [Header("Attack")]
    [SerializeField] private float attackSpeed = 1f;           
    [SerializeField] private float damageWidth = 0.5f;         
    [SerializeField] private float damageHeight = 0.5f;         
    [SerializeField] private float reappearDelay = 1f;          
    [SerializeField] private float resumeDelay = 1f;            
    [SerializeField] private float endImmunityDelay = 1f;       
    [SerializeField] private int healingAmount = 50;           

    [Header("UI & Camera")]
    [SerializeField] private StatusBar healthBar;               
    [SerializeField] private CinemachineVirtualCamera virtualCamera; 
    [SerializeField] private float cameraShakeDuration = 0.5f;  

    // Component references
    private CameraShake cameraShake;        

    // State tracking
    private Vector2 targetPosition;        
    private bool isAttacking = false;      
    private int attackPhase = 0;          
    private float lastAttackTime = 0f;    

    /// <summary>
    /// Initialize boss-specific components and health bar
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        // Set up health bar with max HP
        healthBar.SetMaxValue(maxHP);

        // Get camera shake component
        cameraShake = virtualCamera.GetComponent<CameraShake>();
    }

    /// <summary>
    /// Update boss state every fixed frame
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Update animator with attack state
        animator.SetBool("IsAttacking", isAttacking);

        // Update health bar display
        healthBar.SetValue(currentHp);

        // Show health bar when player is in range
        if (isPlayerInRange)
        {
            healthBar.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Execute attack behavior with multiple attack patterns based on random chance
    /// </summary>
    protected override void AttackPlayer()
    {
        // Check attack cooldown and prevent multiple simultaneous attacks
        if (Time.time - lastAttackTime < attackSpeed || isAttacking) return;

        // Generate random value to determine attack type
        float chance = UnityEngine.Random.Range(0f, 1f);

        // Set attack state and disable movement/flipping
        isAttacking = true;
        canMove = false;
        disableFlip = true;

        // Choose attack based on probability
        if (chance <= 0.3f)                    // 30% chance - Basic attack
        {
            animator.SetTrigger("Attack");
        }
        else if (chance > 0.3f && chance <= 0.5f)  // 20% chance - Split attack
        {
            animator.SetTrigger("Split");
        }
        else if (chance > 0.5f && chance <= 0.6f)  // 10% chance - Buff/Heal
        {
            animator.SetTrigger("Buff");
        }
        else                                   // 40% chance - Special teleport attack
        {
            StartSpecialAttack();
        }

        lastAttackTime = Time.time;
    }

    /// <summary>
    /// Reset boss state after attack completes
    /// Called by animation events
    /// </summary>
    protected override void EndAttack()
    {
        canMove = true;
        isAttacking = false;
        disableFlip = false;
    }

    /// <summary>
    /// Coroutine to reset flip disable after short delay
    /// </summary>
    private IEnumerator ResetFlip()
    {
        yield return new WaitForSeconds(0.5f);
        disableFlip = false;
    }

    /// <summary>
    /// Override damage taking to customize boss damage behavior
    /// Ignores knockback parameters for boss stability
    /// </summary>
    public override void TakeDamage(
            int damage,
            Transform damageSource = null,
            float? knockbackForceX = null,
            float? knockbackForceY = null,
            float? knockbackDuration = null,
            float hitStopDuration = 0.1f)
    {
        // Call base damage method but ignore knockback for boss
        base.TakeDamage(damage, null, null, null, hitStopDuration);
    }

    /// <summary>
    /// Handle boss death - trigger death animation
    /// </summary>
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }

    /// <summary>
    /// Deal damage to player using box cast detection
    /// Called by animation events during attack animations
    /// </summary>
    protected override void Damage()
    {
        // Determine attack direction based on facing direction
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        Vector2 size = new(damageWidth, damageHeight);
        Vector2 origin = (Vector2)attackPoint.position;

        // Cast box to detect player in attack range
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, direction, 0.1f, playerLayer);

        // Apply damage to all hit players
        foreach (var hit in hits)
        {
            var player = hit.collider.GetComponentInParent<Character>();
            if (player != null)
            {
                player.TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
            }
        }
    }

    /// <summary>
    /// Trigger camera shake effect
    /// Called by animation events
    /// </summary>
    private void CamShake()
    {
        cameraShake.Shake(cameraShakeDuration);
    }

    /// <summary>
    /// Initialize special teleport attack sequence
    /// </summary>
    private void StartSpecialAttack()
    {
        StartCoroutine(SpecialAttackRoutine());
    }

    /// <summary>
    /// Load win scene when boss is defeated
    /// Called by animation events or death logic
    /// </summary>
    protected virtual void LoadWinScene()
    {
        SceneManager.LoadScene("GameOver(Win)");
    }

    /// <summary>
    /// Execute complex special attack sequence:
    /// 1. Stop movement and jump
    /// 2. Disappear and become invulnerable
    /// 3. Track player position
    /// 4. Reappear at player's X position
    /// 5. End immunity and resume normal behavior
    /// </summary>
    private IEnumerator SpecialAttackRoutine()
    {
        // Phase 1: Preparation
        Stop();                           // Stop all movement
        disableFlip = true;              // Prevent direction changes
        Jump();                          // Trigger jump animation
        isAttacking = true;              // Set attacking state

        // Phase 2: Disappear
        yield return new WaitForSeconds(reappearDelay);
        GetPlayerPositionX();            // Get target position
        yield return new WaitForSeconds(0.1f);

        // Phase 3: Reappear and attack
        Reappear();                      // Teleport to target position
        EndImmunity();                   // Remove invulnerability
        yield return new WaitForSeconds(endImmunityDelay);

        // Phase 4: Recovery
        isAttacking = false;
        disableFlip = false;
        yield return new WaitForSeconds(resumeDelay);
        Resume();                        // Resume normal AI behavior
    }

    /// <summary>
    /// Enable collision ignoring between boss and player
    /// Used during teleport attack to prevent collision issues
    /// Called by animation events
    /// </summary>
    public void CollisionIgnoreEnabled()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = Character.Instance != null ? Character.Instance.GetComponent<Collider2D>() : null;

        if (myCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(myCollider, playerCollider, true);
        }
    }

    /// <summary>
    /// Disable collision ignoring between boss and player
    /// Called by animation events
    /// </summary>
    public void CollisionIgnoreDisabled()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = Character.Instance != null ? Character.Instance.GetComponent<Collider2D>() : null;

        if (myCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(myCollider, playerCollider, false);
        }
    }

    /// <summary>
    /// Make boss invulnerable by changing to Invincible layer
    /// </summary>
    protected override void Immunity()
    {
        gameObject.layer = LayerMask.NameToLayer("Invincible");
    }

    /// <summary>
    /// Remove boss invulnerability by changing back to Boss layer
    /// </summary>
    protected override void EndImmunity()
    {
        gameObject.layer = LayerMask.NameToLayer("Boss");
    }

    /// <summary>
    /// Trigger jump animation for special attack
    /// Called by animation events
    /// </summary>
    private void Jump()
    {
        animator.SetTrigger("Jump");
    }

    /// <summary>
    /// Make boss disappear and become invulnerable
    /// Called by animation events during special attack
    /// </summary>
    private void Disappear()
    {
        Immunity();                      // Become invulnerable
        rb.velocity = Vector2.zero;      // Stop all movement
        spriteRenderer.enabled = false;  // Hide visual
    }

    /// <summary>
    /// Make boss reappear at target position with impact effect
    /// Called by animation events during special attack
    /// </summary>
    private void Reappear()
    {
        rb.velocity = Vector2.zero;              // Ensure no residual movement
        transform.position = targetPosition;     // Teleport to target
        spriteRenderer.enabled = true;          // Show visual
        animator.SetTrigger("Impact");          // Trigger impact animation
    }

    /// <summary>
    /// Store player's X position for teleport attack targeting
    /// Maintains boss's current Y position
    /// </summary>
    private void GetPlayerPositionX()
    {
        if (Character.Instance != null)
        {
            Vector2 playerPos = Character.Instance.transform.position;
            targetPosition = new Vector2(playerPos.x, transform.position.y);
        }
    }

    /// <summary>
    /// Restore boss health by specified healing amount
    /// Called by animation events during buff/heal attack
    /// </summary>
    private void Heal()
    {
        // Don't heal if at max HP or dead
        if (currentHp >= maxHP || currentHp == 0) return;

        // Clamp healing to max HP
        if (currentHp + healingAmount > maxHP)
        {
            currentHp = maxHP;
        }
        else
        {
            currentHp += healingAmount;
        }
    }

    /// <summary>
    /// Draw debug gizmos for attack hitbox visualization in editor
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw attack hitbox
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, new Vector2(damageWidth, damageHeight));
        }
    }
}
