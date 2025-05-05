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

        // Internal state
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
        protected virtual void Awake()
        {
            current_player = Character.Instance;
            currentHp = maxHP;
            spriteRenderer.material.SetColor("_HitColor", hitColor);
        }

        protected virtual void FixedUpdate()
        {
            if (currentHp <= 0)
            {
                Die();
                return;
            }

            if (current_player == null)
            {
                current_player = Character.Instance;
                if (current_player == null)
                {
                    rb.velocity = Vector2.zero;  
                    currentSpeed = 0;
                    animator.SetFloat("Speed", 0); 
                    return;
                }
            }
            if (isKnockedBack)
            {
                currentSpeed = 0;
                animator.SetFloat("Speed", 0);
                return;
            }
            CheckAndJumpOverObstacle();
            FollowPlayer();
            InAttackRange();
            FlipLogic();

            // Update animator speed based on actual velocity
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            Gizmos.DrawWireSphere(transform.position, followRange);
            if (!obstacleCheckPoint) return;

            Gizmos.color = Color.cyan;
            Vector2 rayDir = facingRight ? Vector2.right : Vector2.left;
            Gizmos.DrawRay(obstacleCheckPoint.position, rayDir * obstacleCheckDistance);
        }
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            isGrounded = true;
        }



        //================== MOVEMENT ==================//
        protected virtual void FollowPlayer()
        {
            if (!canMove || current_player == null)
            {
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, current_player.transform.position);
            if (distanceToPlayer > followRange)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);  
                currentSpeed = 0;
                animator.SetFloat("Speed", 0);
                isPlayerInRange = false;
                return;
            }
            isPlayerInRange = true;
            Vector2 direction = (current_player.transform.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
            currentSpeed = moveSpeed;
        }

        protected virtual void FlipLogic()
        {
            if (disableFlip || current_player == null) return;

            Vector2 dir = (current_player.transform.position - transform.position).normalized;
            if ((dir.x > 0 && !facingRight) || (dir.x < 0 && facingRight))
                Flip();
        }

        protected void Flip()
        {
            facingRight = !facingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        protected virtual void Stop()
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            currentSpeed = 0;
            canAttack = false;
            canMove = false;
        }
        protected virtual void Resume()
        {
            canAttack = true;
            canMove = true;
        }

        //================== JUMP ==================//
        protected virtual void CheckAndJumpOverObstacle()
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
            if (hit.collider != null && isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                isGrounded = false; 
            }
        }

        //================== COMBAT ==================//
        protected virtual void InAttackRange()
        {
            if (!canAttack || attackPoint == null) return;

            if (Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer).Any())
            {
                canMove = false;
                AttackPlayer();
            }
        }

        protected virtual void Damage()
        {
            var hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);
            if (hitPlayers == null || hitPlayers.Length == 0) return;

            foreach (var p in hitPlayers)
            {
                p.GetComponent<Character>()?.TakeDamage(attackDamage, transform, knockbackForceX, knockbackForceY,knockbackDuration);
            }
        }

        protected virtual void EndAttack()
        {
            canMove = true;
        }

        protected abstract void AttackPlayer();

        //================== DAMAGE SYSTEM ==================//
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

            currentHp -= damage;

            spriteRenderer.material.SetFloat("_Opacity", 1f);
            StartCoroutine(ResetOpacity());
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
            spriteRenderer.material.SetFloat("_Opacity",0f);
        }
        protected virtual void ApplyKnockback(Transform source, float forceX, float forceY, float duration)
        {
            Stop();
            if (rb == null) return;

            float knockbackDirectionX = (source.position.x < transform.position.x) ? 1f : -1f;

            // Set the knockback vector
            Vector2 knockbackVector = new Vector2(knockbackDirectionX * forceX, forceY);

            // Reset velocity before applying knockback
            rb.velocity = Vector2.zero;
            rb.AddForce(knockbackVector, ForceMode2D.Impulse);  

            if (duration > 0f)
            {
                StartCoroutine(ResetKnockbackAfterDelay(duration));
            }
        }

        protected virtual IEnumerator ResetKnockbackAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            canAttack = true;
            canMove = true;
            isKnockedBack = false;
            currentSpeed = moveSpeed;
        }

        //================== DEATH ==================//
        protected virtual void Die() { }
        protected virtual void DestroyObject() => Destroy(gameObject);

        //================== IMMUNITY ==================//
        protected virtual void Immunity()
        {
            gameObject.layer = LayerMask.NameToLayer("Invincible");
            canMove = false;
            canAttack = false;
        }

        protected virtual void EndImmunity()
        {
            gameObject.layer = LayerMask.NameToLayer("Enemy");
            canMove = true;
            canAttack = true;
        }

        //================== HIT STOP ==================//
        protected IEnumerator FreezeCoroutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }
    }
}