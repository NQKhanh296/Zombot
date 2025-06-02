using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Scripts
{
    public abstract class Enemy : MonoBehaviour, IDamageable
    {
        [Header("Movement")]
        public float followRange;
        public float moveSpeed;
        public Transform attackPoint;
        public float attackRange;
        public LayerMask playerLayer;

        [Header("Auto-Jump")]
        public float jumpForce = 12f;
        public Transform obstacleCheckPoint;
        public float obstacleCheckDistance = 1f;
        public LayerMask groundLayer;

        [Header("Combat")]
        public int maxHP;
        public int attackDamage;
        public float knockbackForceX;
        public float knockbackForceY;
        public float knockbackDuration;

        [Header("Components")]
        public Rigidbody2D rb;
        public Animator animator;
        public BoxCollider2D boxCollider2D;
        public SpriteRenderer spriteRenderer;
        public Color hitColor = Color.white;
        public float takeHitDuration = 0.1f;

        protected Character current_player;
        protected bool facingRight = false;
        protected bool canAttack = true;
        protected bool canMove = true;
        protected bool disableFlip = false;
        protected float currentSpeed = 0;
        protected bool isKnockedBack = false;
        protected bool isGrounded = false;
        protected int currentHp;
        protected bool isPlayerInRange = false;

        //================== UNITY ==================//

        /// <summary>
        /// Initialize enemy state and components
        /// Sets up player reference, health, and material properties
        /// </summary>
        protected virtual void Awake()
        {
            current_player = Character.Instance;
            currentHp = maxHP;
            spriteRenderer.material.SetColor("_HitColor", hitColor);
        }

        /// <summary>
        /// Main update loop for enemy behavior
        /// Handles death, player following, combat, and animation updates
        /// </summary>
        protected virtual void FixedUpdate()
        {
            // Handle death state
            if (currentHp <= 0)
            {
                Die();
                return;
            }

            // Ensure player reference is valid
            if (current_player == null)
            {
                current_player = Character.Instance;
                if (current_player == null)
                {
                    // Stop all movement if no player found
                    rb.velocity = Vector2.zero;
                    currentSpeed = 0;
                    animator.SetFloat("Speed", 0);
                    return;
                }
            }

            // Skip movement updates during knockback
            if (isKnockedBack)
            {
                currentSpeed = 0;
                animator.SetFloat("Speed", 0);
                return;
            }

            // Execute core AI behaviors
            CheckAndJumpOverObstacle();
            FollowPlayer();
            InAttackRange();
            FlipLogic();

            // Update animator with current movement speed
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        }

        /// <summary>
        /// Draw debug gizmos in Scene view for visualization
        /// Shows attack range, follow range, and obstacle detection rays
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;

            // Draw attack range
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);

            // Draw follow range
            Gizmos.DrawWireSphere(transform.position, followRange);

            if (!obstacleCheckPoint) return;

            // Draw obstacle detection ray
            Gizmos.color = Color.cyan;
            Vector2 rayDir = facingRight ? Vector2.right : Vector2.left;
            Gizmos.DrawRay(obstacleCheckPoint.position, rayDir * obstacleCheckDistance);
        }

        /// <summary>
        /// Handle collision events, primarily for ground detection
        /// </summary>
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            isGrounded = true;
        }

        //================== MOVEMENT ==================//

        /// <summary>
        /// Controls enemy movement toward the player
        /// Stops movement if player is out of range or movement is disabled
        /// </summary>
        protected virtual void FollowPlayer()
        {
            if (!canMove || current_player == null)
            {
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, current_player.transform.position);

            // Stop following if player is too far away
            if (distanceToPlayer > followRange)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                currentSpeed = 0;
                animator.SetFloat("Speed", 0);
                isPlayerInRange = false;
                return;
            }

            // Move toward player when in range
            isPlayerInRange = true;
            Vector2 direction = (current_player.transform.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
            currentSpeed = moveSpeed;
        }

        /// <summary>
        /// Handles sprite flipping to face the player
        /// Automatically rotates the enemy to face the direction of the player
        /// </summary>
        protected virtual void FlipLogic()
        {
            if (disableFlip || current_player == null) return;

            Vector2 dir = (current_player.transform.position - transform.position).normalized;

            // Flip if facing wrong direction
            if ((dir.x > 0 && !facingRight) || (dir.x < 0 && facingRight))
                Flip();
        }

        /// <summary>
        /// Flips the enemy sprite horizontally
        /// Updates the facingRight flag and rotates the transform
        /// </summary>
        protected void Flip()
        {
            facingRight = !facingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        /// <summary>
        /// Stops enemy movement and disables attack/movement capabilities
        /// Used during knockback or special states
        /// </summary>
        protected virtual void Stop()
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            currentSpeed = 0;
            canAttack = false;
            canMove = false;
        }

        /// <summary>
        /// Resumes normal enemy behavior after being stopped
        /// Re-enables attack and movement capabilities
        /// </summary>
        protected virtual void Resume()
        {
            canAttack = true;
            canMove = true;
        }

        //================== JUMP ==================//

        /// <summary>
        /// Automatically makes the enemy jump over obstacles in its path
        /// Uses raycast detection to identify obstacles and applies jump force when grounded
        /// </summary>
        protected virtual void CheckAndJumpOverObstacle()
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

            // Jump over obstacle if detected and enemy is grounded
            if (hit.collider != null && isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                isGrounded = false;
            }
        }

        //================== COMBAT ==================//

        /// <summary>
        /// Checks if player is within attack range and initiates attack
        /// Stops movement when attacking and calls abstract AttackPlayer method
        /// </summary>
        protected virtual void InAttackRange()
        {
            if (!canAttack || attackPoint == null) return;

            // Check if any player is within attack range
            if (Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer).Any())
            {
                canMove = false;
                AttackPlayer();
            }
        }

        /// <summary>
        /// Base damage method that hits all players within attack range
        /// Can be overridden by derived classes for custom damage behavior
        /// </summary>
        protected virtual void Damage()
        {
            var hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);
            if (hitPlayers == null || hitPlayers.Length == 0) return;

            // Apply damage to all players in range
            foreach (var p in hitPlayers)
            {
                p.GetComponent<Character>()?.TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
            }
        }

        /// <summary>
        /// Called at the end of attack animations to resume movement
        /// Animation event callback method
        /// </summary>
        protected virtual void EndAttack()
        {
            canMove = true;
        }

        /// <summary>
        /// Abstract method that must be implemented by derived enemy classes
        /// Defines the specific attack behavior for each enemy type
        /// </summary>
        protected abstract void AttackPlayer();

        //================== DAMAGE SYSTEM ==================//

        /// <summary>
        /// Main damage handling method implementing IDamageable interface
        /// Applies damage, visual effects, knockback, and hit stop effects
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="damageSource">Transform of the damage source for knockback direction</param>
        /// <param name="knockbackForceX">Optional horizontal knockback force</param>
        /// <param name="knockbackForceY">Optional vertical knockback force</param>
        /// <param name="knockbackDuration">Optional duration of knockback effect</param>
        /// <param name="hitStopDuration">Duration of hit stop effect (time freeze)</param>
        public virtual void TakeDamage
            (
            int damage,
            Transform damageSource = null,
            float? knockbackForceX = null,
            float? knockbackForceY = null,
            float? knockbackDuration = null,
            float hitStopDuration = 0.1f
            )
        {
            // Apply damage to health
            currentHp -= damage;

            // Trigger hit visual effect
            spriteRenderer.material.SetFloat("_Opacity", 1f);
            StartCoroutine(ResetOpacity());

            // Apply hit stop effect if specified
            if (hitStopDuration > 0)
            {
                StartCoroutine(FreezeCoroutine(hitStopDuration));
            }

            // Apply knockback if damage source and forces are provided
            if (damageSource != null && (knockbackForceX.HasValue || knockbackForceY.HasValue))
            {
                ApplyKnockback(damageSource, knockbackForceX ?? 0f, knockbackForceY ?? 0f, knockbackDuration ?? 0f);
            }
        }

        /// <summary>
        /// Resets the visual hit effect after the specified duration
        /// Coroutine that handles the hit flash animation
        /// </summary>
        protected IEnumerator ResetOpacity()
        {
            yield return new WaitForSeconds(takeHitDuration);
            spriteRenderer.material.SetFloat("_Opacity", 0f);
        }

        /// <summary>
        /// Applies knockback physics to the enemy
        /// Calculates direction based on damage source position and applies forces
        /// </summary>
        /// <param name="source">Transform of the damage source</param>
        /// <param name="forceX">Horizontal knockback force</param>
        /// <param name="forceY">Vertical knockback force</param>
        /// <param name="duration">Duration of knockback state</param>
        protected virtual void ApplyKnockback(Transform source, float forceX, float forceY, float duration)
        {
            Stop();
            if (rb == null) return;

            // Calculate knockback direction based on source position
            float knockbackDirectionX = (source.position.x < transform.position.x) ? 1f : -1f;

            // Create and apply knockback vector
            Vector2 knockbackVector = new Vector2(knockbackDirectionX * forceX, forceY);

            rb.velocity = Vector2.zero;
            rb.AddForce(knockbackVector, ForceMode2D.Impulse);

            // Handle knockback duration
            if (duration > 0f)
            {
                StartCoroutine(ResetKnockbackAfterDelay(duration));
            }
            else
            {
                Resume();
            }
        }

        /// <summary>
        /// Resets knockback state after the specified delay
        /// Coroutine that restores normal enemy behavior after knockback
        /// </summary>
        /// <param name="delay">Time to wait before resetting knockback state</param>
        protected virtual IEnumerator ResetKnockbackAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            canAttack = true;
            canMove = true;
            isKnockedBack = false;
            currentSpeed = moveSpeed;
        }

        //================== DEATH ==================//

        /// <summary>
        /// Abstract death method to be implemented by derived classes
        /// Called when enemy health reaches zero or below
        /// </summary>
        protected virtual void Die() { }

        /// <summary>
        /// Destroys the enemy game object
        /// Called after death animations or effects are complete
        /// </summary>
        protected virtual void DestroyObject() => Destroy(gameObject);

        //================== IMMUNITY ==================//

        /// <summary>
        /// Grants temporary immunity to the enemy
        /// Changes layer to prevent damage and disables movement/attacks
        /// </summary>
        protected virtual void Immunity()
        {
            gameObject.layer = LayerMask.NameToLayer("Invincible");
            canMove = false;
            canAttack = false;
        }

        /// <summary>
        /// Ends immunity state and restores normal behavior
        /// Returns enemy to normal layer and re-enables abilities
        /// </summary>
        protected virtual void EndImmunity()
        {
            gameObject.layer = LayerMask.NameToLayer("Enemy");
            canMove = true;
            canAttack = true;
        }

        //================== HIT STOP ==================//

        /// <summary>
        /// Creates a hit stop effect by temporarily freezing time
        /// Used to create impact feedback during attacks and damage
        /// </summary>
        /// <param name="duration">Duration of the time freeze effect</param>
        protected IEnumerator FreezeCoroutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }
    }
}