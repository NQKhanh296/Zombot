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
        public float moveSpeed = 400;
        protected float currentSpeed = 0;
        public int maxHealth = 100;
        protected int currentHealth;
        public int jumpStrength = 15;
        public Rigidbody2D rb;
        public Animator animator;
        public LayerMask groundLayer;
        public float groundCheckDistance = 0.2f;
        public StatusBar healthBar;
        public SpriteRenderer spriteRenderer;
        public Color hitColor = Color.white;
        public float takeHitDuration = 0.1f;

        protected bool facingRight = true;
        protected bool isJumping = false;
        protected bool isFalling = false;
        protected bool isGrounded = false;
        protected bool controlDisabled = false;
        protected bool isAttacking = false;
        protected bool isAlive = true;
        protected MaterialPropertyBlock propertyBlock;
        public static Character Instance { get; private set; }
        protected virtual void Awake()
        {
            healthBar.gameObject.SetActive(true);
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_HitColor", hitColor);
            propertyBlock.SetFloat("_Opacity", 0f);
            spriteRenderer.SetPropertyBlock(propertyBlock);
            if (Instance == null)
                Instance = this;
            else
                Debug.LogWarning("Multiple Character instances in scene!");
            currentHealth = maxHealth;
            currentSpeed = moveSpeed;
            healthBar.SetMaxValue(maxHealth);
        }
        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        protected void Move()
        {
            if (controlDisabled)
            {
                return;
            }
            float horizontalMove = Input.GetAxisRaw("Horizontal") * currentSpeed;
            animator.SetFloat("Speed", Mathf.Abs(horizontalMove));
            rb.velocity = new Vector2(horizontalMove * Time.deltaTime, rb.velocity.y);
            if (horizontalMove > 0 && !facingRight)
            {
                Flip();
            }
            else if (horizontalMove < 0 && facingRight)
            {
                Flip();
            }
        }
        protected void Flip()
        {
            facingRight = !facingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        protected void Jump()
        {
            if (Input.GetKey(KeyCode.Space) && isGrounded && !controlDisabled)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpStrength);
                isGrounded = false;
                isJumping = true;
                isFalling = false;
            }
            if (!isGrounded && rb.velocity.y < 0)
            {
                isFalling = true;
                isJumping = false;
            }
            if (isGrounded)
            {
                isJumping = false;  
                isFalling = false;  
            }
        }
        protected virtual void GroundCheck()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
            if (hit.collider != null)
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }

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
            if (!isAlive) return;
            propertyBlock.SetFloat("_Opacity", 1f);
            spriteRenderer.SetPropertyBlock(propertyBlock);
            StartCoroutine(ResetOpacity());


            currentHealth -= damage;
            if (hitStopDuration > 0)
            {
                StartCoroutine(FreezeCoroutine(hitStopDuration));
            }

            if (damageSource != null && (knockbackForceX.HasValue || knockbackForceY.HasValue))
            {
                ApplyKnockback(damageSource, knockbackForceX ?? 0f, knockbackForceY ?? 0f, knockbackDuration ?? 0f);
            }
        }

        protected IEnumerator ResetOpacity()
        {
            yield return new WaitForSeconds(takeHitDuration);
            propertyBlock.SetFloat("_Opacity", 0f);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }

        protected virtual void ApplyKnockback(Transform source, float forceX, float forceY, float duration)
        {
            controlDisabled = true;
            if (rb == null) return;

            float knockbackDirectionX = (source.position.x < transform.position.x) ? 1f : -1f;

            // Set the knockback vector
            Vector2 knockbackVector = new Vector2(knockbackDirectionX * forceX, forceY);

            // Reset velocity before applying knockback
            rb.velocity = Vector2.zero;
            rb.AddForce(knockbackVector, ForceMode2D.Impulse);

            if (duration > 0f)
            {
                StartCoroutine(DisableMovementForSeconds(duration));
            }
        }
        protected virtual IEnumerator DisableMovementForSeconds(float duration)
        {
            yield return new WaitForSeconds(duration);
            rb.velocity = Vector2.zero;
            controlDisabled = false;
        }
        protected IEnumerator FreezeCoroutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }
        protected abstract void Attack();
        protected virtual void Die(){}
        protected virtual void DestroyObject() 
        {
            Destroy(gameObject);
        }
        protected virtual void LoadLoseScene()
        {
            SceneManager.LoadScene("GameOver(Lose)");
        }
        protected virtual void FixedUpdate()
        {
            if (!isAlive) return;
            Move();
            Jump();
            Attack();
            GroundCheck();
            if (currentHealth <= 0)
            {
                isAlive = false;
                Die();
            }
            healthBar.SetValue(currentHealth);
            animator.SetBool("IsJumping", isJumping);
            animator.SetBool("IsFalling", isFalling);
            animator.SetBool("IsAlive", isAlive);
        }

        public virtual Transform GetTransform()
        {
            return transform;
        }

        public bool IsActive()
        {
            return gameObject.activeInHierarchy;
        }
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + Vector2.down * groundCheckDistance);
        }
    }
}
