using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public abstract class Character : MonoBehaviour, IDamageable
    {
        #region Public Fields - Character Stats
        [Header("Movement Settings")]
        public float moveSpeed = 400;           // Base movement speed of the character
        public int jumpStrength = 15;           // Force applied when jumping

        [Header("Health Settings")]
        public int maxHealth = 100;             // Maximum health points

        [Header("Physics & Animation")]
        public Rigidbody2D rb;                  // Physics body component
        public Animator animator;               // Animation controller

        [Header("Ground Detection")]
        public LayerMask groundLayer;           // Layer mask for ground detection
        public float groundCheckDistance = 0.2f; // Distance to check for ground below character

        [Header("UI & Visual Feedback")]
        public StatusBar healthBar;             // Health bar UI component
        public SpriteRenderer spriteRenderer;  // Sprite renderer for visual effects
        public Color hitColor = Color.white;    // Color applied when taking damage
        public float takeHitDuration = 0.1f;    // Duration of hit effect visibility
        #endregion

        #region Protected Fields - Internal State
        protected float currentSpeed = 0;       // Current movement speed (can be modified by effects)
        protected int currentHealth;            // Current health points
        protected bool facingRight = true;      // Direction the character is facing
        protected bool isJumping = false;       // True when character is in upward jump motion
        protected bool isFalling = false;       // True when character is falling downward
        protected bool isGrounded = false;      // True when character is touching ground
        protected bool controlDisabled = false; // True when player input is disabled (knockback, etc.)
        protected bool isAttacking = false;     // True when character is performing an attack
        protected bool isAlive = true;          // False when character has died
        protected MaterialPropertyBlock propertyBlock; // Used for shader property manipulation
        #endregion

        #region Singleton Pattern
        /// <summary>
        /// Singleton instance of the Character class for global access
        /// </summary>
        public static Character Instance { get; private set; }
        #endregion

        #region Unity Lifecycle Methods
        /// <summary>
        /// Initialize character components and set up initial state
        /// </summary>
        protected virtual void Awake()
        {
            // Enable health bar UI
            healthBar.gameObject.SetActive(true);

            // Initialize material property block for hit effects
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_HitColor", hitColor);
            propertyBlock.SetFloat("_Opacity", 0f);
            spriteRenderer.SetPropertyBlock(propertyBlock);

            // Set up singleton pattern
            if (Instance == null)
                Instance = this;
            else
                Debug.LogWarning("Multiple Character instances in scene!");

            // Initialize character stats
            currentHealth = maxHealth;
            currentSpeed = moveSpeed;
            healthBar.SetMaxValue(maxHealth);
        }

        /// <summary>
        /// Clean up singleton reference when character is destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Main update loop - handles all character behavior each physics frame
        /// </summary>
        protected virtual void FixedUpdate()
        {
            // Skip all updates if character is dead
            if (!isAlive) return;

            // Execute core character behaviors
            Move();
            Jump();
            Attack();
            GroundCheck();

            // Check for death condition
            if (currentHealth <= 0)
            {
                isAlive = false;
                Die();
            }

            // Update UI and animation states
            healthBar.SetValue(currentHealth);
            animator.SetBool("IsJumping", isJumping);
            animator.SetBool("IsFalling", isFalling);
            animator.SetBool("IsAlive", isAlive);
        }
        #endregion

        #region Movement System
        /// <summary>
        /// Handles horizontal movement based on player input
        /// Updates animation and manages character facing direction
        /// </summary>
        protected void Move()
        {
            // Don't move if controls are disabled (during knockback, etc.)
            if (controlDisabled)
            {
                return;
            }

            // Get horizontal input and calculate movement
            float horizontalMove = Input.GetAxisRaw("Horizontal") * currentSpeed;

            // Update animation speed parameter
            animator.SetFloat("Speed", Mathf.Abs(horizontalMove));

            // Apply movement to rigidbody (preserving vertical velocity)
            rb.velocity = new Vector2(horizontalMove * Time.deltaTime, rb.velocity.y);

            // Handle character facing direction
            if (horizontalMove > 0 && !facingRight)
            {
                Flip();
            }
            else if (horizontalMove < 0 && facingRight)
            {
                Flip();
            }
        }

        /// <summary>
        /// Flips the character sprite to face the opposite direction
        /// </summary>
        protected void Flip()
        {
            facingRight = !facingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        /// <summary>
        /// Handles jump input and manages jumping/falling states
        /// </summary>
        protected void Jump()
        {
            // Execute jump if conditions are met
            if (Input.GetKey(KeyCode.Space) && isGrounded && !controlDisabled)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpStrength);
                isGrounded = false;
                isJumping = true;
                isFalling = false;
            }

            // Detect falling state (moving downward while airborne)
            if (!isGrounded && rb.velocity.y < 0)
            {
                isFalling = true;
                isJumping = false;
            }

            // Reset jump/fall states when grounded
            if (isGrounded)
            {
                isJumping = false;
                isFalling = false;
            }
        }

        /// <summary>
        /// Performs raycast to detect if character is touching ground
        /// </summary>
        protected virtual void GroundCheck()
        {
            // Cast ray downward to detect ground
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);

            // Update grounded state based on raycast result
            if (hit.collider != null)
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }
        #endregion

        #region Damage System
        /// <summary>
        /// Handles damage application with optional knockback and hit effects
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="damageSource">Transform of the damage source for knockback direction</param>
        /// <param name="knockbackForceX">Horizontal knockback force</param>
        /// <param name="knockbackForceY">Vertical knockback force</param>
        /// <param name="knockbackDuration">Duration of knockback effect</param>
        /// <param name="hitStopDuration">Duration of time freeze effect</param>
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
            // Don't take damage if already dead
            if (!isAlive) return;

            // Apply visual hit effect
            propertyBlock.SetFloat("_Opacity", 1f);
            spriteRenderer.SetPropertyBlock(propertyBlock);
            StartCoroutine(ResetOpacity());

            // Apply damage to health
            currentHealth -= damage;

            // Apply hit stop effect (brief time freeze)
            if (hitStopDuration > 0)
            {
                StartCoroutine(FreezeCoroutine(hitStopDuration));
            }

            // Apply knockback if specified
            if (damageSource != null && (knockbackForceX.HasValue || knockbackForceY.HasValue))
            {
                ApplyKnockback(damageSource, knockbackForceX ?? 0f, knockbackForceY ?? 0f, knockbackDuration ?? 0f);
            }
        }

        /// <summary>
        /// Coroutine to reset the hit effect opacity after specified duration
        /// </summary>
        protected IEnumerator ResetOpacity()
        {
            yield return new WaitForSeconds(takeHitDuration);
            propertyBlock.SetFloat("_Opacity", 0f);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>
        /// Applies knockback force to the character based on damage source position
        /// </summary>
        /// <param name="source">Transform of the damage source</param>
        /// <param name="forceX">Horizontal knockback force</param>
        /// <param name="forceY">Vertical knockback force</param>
        /// <param name="duration">Duration to disable player control</param>
        protected virtual void ApplyKnockback(Transform source, float forceX, float forceY, float duration)
        {
            // Disable player control during knockback
            controlDisabled = true;
            if (rb == null) return;

            // Determine knockback direction based on source position
            float knockbackDirectionX = (source.position.x < transform.position.x) ? 1f : -1f;

            // Calculate and apply knockback force
            Vector2 knockbackVector = new Vector2(knockbackDirectionX * forceX, forceY);
            rb.velocity = Vector2.zero; // Reset velocity before applying knockback
            rb.AddForce(knockbackVector, ForceMode2D.Impulse);

            // Start coroutine to re-enable control after duration
            if (duration > 0f)
            {
                StartCoroutine(DisableMovementForSeconds(duration));
            }
        }

        /// <summary>
        /// Coroutine to temporarily disable movement for specified duration
        /// </summary>
        /// <param name="duration">Time in seconds to disable movement</param>
        protected virtual IEnumerator DisableMovementForSeconds(float duration)
        {
            yield return new WaitForSeconds(duration);
            rb.velocity = Vector2.zero; // Stop any remaining movement
            controlDisabled = false;    // Re-enable player control
        }

        /// <summary>
        /// Creates a brief time freeze effect for dramatic impact
        /// </summary>
        /// <param name="duration">Duration of the freeze in seconds</param>
        protected IEnumerator FreezeCoroutine(float duration)
        {
            Time.timeScale = 0f; // Stop time
            yield return new WaitForSecondsRealtime(duration); // Wait in real time
            Time.timeScale = 1f; // Resume normal time
        }
        #endregion

        #region Abstract and Virtual Methods
        /// <summary>
        /// Abstract method for attack behavior - must be implemented by derived classes
        /// </summary>
        protected abstract void Attack();

        /// <summary>
        /// Virtual method called when character dies - can be overridden by derived classes
        /// </summary>
        protected virtual void Die() { }

        /// <summary>
        /// Virtual method to destroy the character GameObject
        /// </summary>
        protected virtual void DestroyObject()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Virtual method to load the game over scene
        /// </summary>
        protected virtual void LoadLoseScene()
        {
            SceneManager.LoadScene("GameOver(Lose)");
        }
        #endregion

        #region Public Interface Methods
        /// <summary>
        /// Returns the character's transform component
        /// </summary>
        /// <returns>Transform component of this character</returns>
        public virtual Transform GetTransform()
        {
            return transform;
        }

        /// <summary>
        /// Checks if the character GameObject is active in the scene
        /// </summary>
        /// <returns>True if active, false otherwise</returns>
        public bool IsActive()
        {
            return gameObject.activeInHierarchy;
        }
        #endregion

        #region Debug Visualization
        /// <summary>
        /// Draws debug gizmos in the Scene view to visualize ground check distance
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + Vector2.down * groundCheckDistance);
        }
        #endregion
    }
}
