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
    public float attackSpeed = 0.5f;
    public float damageWidth = 0.5f;
    public float damageHeight = 0.5f;
    public float reappearDelay = 1f;
    public float resumeDelay = 1f;
    public float endImmunityDelay = 1f;
    public int healingAmount = 50;
    public StatusBar healthBar;
    public CinemachineVirtualCamera virtualCamera;
    public float cameraShakeDuration = 0.5f;
    protected CameraShake cameraShake;
    protected Vector2 targetPosition;
    protected bool isAttacking = false;
    protected int attackPhase = 0;
    protected float lastAttackTime = 0f;
    protected override void Awake()
    {
        base.Awake();
        healthBar.SetMaxValue(maxHP);
        cameraShake = virtualCamera.GetComponent<CameraShake>();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        animator.SetBool("IsAttacking", isAttacking);
        healthBar.SetValue(currentHp);
        if (isPlayerInRange)
        {
            healthBar.gameObject.SetActive(true);
        }
    }
    protected override void AttackPlayer()
    {
        if (Time.time - lastAttackTime < attackSpeed || isAttacking) return;

        float chance = UnityEngine.Random.Range(0f, 1f);

        isAttacking = true;
        canMove = false;
        disableFlip = true;

        if (chance <= 0.3f)
        {
            animator.SetTrigger("Attack");
        }
        else if (chance > 0.3f && chance <= 0.5f)
        {
            animator.SetTrigger("Split");
        }
        else if (chance > 0.5f && chance <= 0.6f)
        {
            animator.SetTrigger("Buff");
        }
        else
        {
            StartSpecialAttack();
        }

        lastAttackTime = Time.time;
    }
    protected override void EndAttack()
    {
        canMove = true;
        isAttacking = false;
        disableFlip = false;
    }
    private IEnumerator ResetFlip()
    {
        yield return new WaitForSeconds(0.5f);
        disableFlip = false;
    }
    public override void TakeDamage(
            int damage,
            Transform damageSource = null,
            float? knockbackForceX = null,
            float? knockbackForceY = null,
            float? knockbackDuration = null,
            float hitStopDuration = 0.1f)
    {
        base.TakeDamage(damage, null, null, null, hitStopDuration);
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
    private void CamShake()
    {
        cameraShake.Shake(cameraShakeDuration);
    }
    private void StartSpecialAttack()
    {
        StartCoroutine(SpecialAttackRoutine());
    }
    protected virtual void LoadWinScene()
    {
        SceneManager.LoadScene("GameOver(Win)");
    }
    private IEnumerator SpecialAttackRoutine()
    {
        Stop();
        disableFlip = true;
        Jump();
        isAttacking = true;
        yield return new WaitForSeconds(reappearDelay);
        GetPlayerPositionX();
        yield return new WaitForSeconds(0.1f); 

        Reappear();
        EndImmunity();
        yield return new WaitForSeconds(endImmunityDelay);
        isAttacking = false;
        disableFlip = false;
        yield return new WaitForSeconds(resumeDelay);
        Resume();
    }
    public void CollisionIgnoreEnabled()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = Character.Instance != null ? Character.Instance.GetComponent<Collider2D>() : null;
        if (myCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(myCollider, playerCollider, true);
        }
    }
    public void CollisionIgnoreDisabled()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = Character.Instance != null ? Character.Instance.GetComponent<Collider2D>() : null;
        if (myCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(myCollider, playerCollider, false);
        }
    }
    protected override void Immunity()
    {
        gameObject.layer = LayerMask.NameToLayer("Invincible");
    }

    protected override void EndImmunity()
    {
        gameObject.layer = LayerMask.NameToLayer("Boss");
    }
    private void Jump()
    {
        animator.SetTrigger("Jump");
    }
    private void Disappear()
    {
        Immunity();
        rb.velocity = Vector2.zero;
        spriteRenderer.enabled = false;
    }
    private void Reappear()
    {
        rb.velocity = Vector2.zero;
        transform.position = targetPosition;
        spriteRenderer.enabled = true;
        animator.SetTrigger("Impact");
    }

    private void GetPlayerPositionX()
    {
        if (Character.Instance != null)
        {
            Vector2 playerPos = Character.Instance.transform.position;
            targetPosition = new Vector2(playerPos.x, transform.position.y);
        }
    }
    
    private void Heal()
    {
        if (currentHp >= maxHP || currentHp == 0) return;
        if (currentHp + healingAmount > maxHP)
        {
            currentHp = maxHP;
        }
        else
        {
            currentHp += healingAmount;
        }
    }
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, new Vector2(damageWidth, damageHeight));
        }
    }
}
