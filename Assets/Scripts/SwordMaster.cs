using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordMaster : Character
{
    private int attackPhase = 0;
    private float lastAttackTime = 0f;
    public float attackSpeed = 0.5f;
    public Transform attackPoint;
    public float damageWidth = 0.5f;
    public float damageHeight = 0.5f; 
    public LayerMask enemyLayer;
    public LayerMask collidableLayers;
    public int attackDamage = 40;
    private bool canDash = true;
    private bool isDashing;
    public float dashDistance = 10f;
    public float dashSpeed = 10f;
    public float dashCooldown = 1f;
    public StatusBar dashBar;
    public int dashbarMaxValue = 1;

    protected override void Awake()
    {
        base.Awake();
        dashBar.gameObject.SetActive(true);
        dashBar.SetMaxValue(dashbarMaxValue);
        dashBar.SetValue(dashbarMaxValue);
    }
    
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        TriggerDash();
        animator.SetBool("IsAttacking",isAttacking);
        animator.SetBool("IsDashing",isDashing);
    }



    protected override void Attack()
    {
        if (controlDisabled)
        {
            return;
        }
        if (Input.GetKey(KeyCode.J) && (Time.time - lastAttackTime > attackSpeed))
        {
            isAttacking = true;
            attackPhase = (attackPhase % 4) + 1;
            animator.SetTrigger($"Attack{attackPhase}");
            lastAttackTime = Time.time;
        }
    }
    private void ResetAttack()
    {
        isAttacking = false;
    }   
    protected void Damage()
    {
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        Vector2 size = new(damageWidth, damageHeight);
        Vector2 origin = (Vector2)attackPoint.position;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, direction, 0.1f, enemyLayer);

        foreach (var hit in hits)
        {
            var enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage, transform, 10f, 10f,0.6f);
            }
        }
    }
    private void TriggerDash()
    {
        if (controlDisabled)
        {
            return;
        }
        if (Input.GetKey(KeyCode.LeftShift) && canDash)
        {
            animator.SetTrigger("Dash");
            StartCoroutine(Dash());
        }
    }
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        controlDisabled = true;
        rb.velocity = Vector2.zero;
        dashBar.SetValue(0);

        float originalGravity = rb.gravityScale;
        Immunity();
        rb.gravityScale = 0f;
        Vector2 dashDir = facingRight ? Vector2.right : Vector2.left;
        float dashDuration = dashDistance / dashSpeed;
        float elapsed = 0f;

        currentSpeed = 0;

        while (elapsed < dashDuration)
        {
            float step = dashSpeed * Time.fixedDeltaTime;
            Vector2 nextPos = rb.position + dashDir * step;
            Vector2 boxSize = GetComponent<Collider2D>().bounds.size;
            boxSize.y = Mathf.Max(0.01f, boxSize.y - 1f);
            boxSize.x = 0.01f;

            RaycastHit2D hit = Physics2D.BoxCast(rb.position, boxSize, 0f, dashDir, step, collidableLayers);
            if (hit.collider != null)
            {
                break; 
            }

            rb.MovePosition(nextPos);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate(); 
        }

        rb.gravityScale = originalGravity;
        isDashing = false;
        controlDisabled = false;

        yield return new WaitForSeconds(0.1f);
        currentSpeed = moveSpeed;
        EndImmunity();

        float refillElapsed = 0f;

        while (refillElapsed < dashCooldown)
        {
            float t = refillElapsed / dashCooldown;  
            dashBar.SetValue(t);  
            refillElapsed += Time.deltaTime;  
            yield return null;  
        }

        dashBar.SetValue(1);

        canDash = true;
    }
    

    private void Immunity()
    {
        gameObject.layer = LayerMask.NameToLayer("Invincible");
    }
    private void EndImmunity()
    {
        gameObject.layer = LayerMask.NameToLayer("Player");
    }
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }
    protected override void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, new Vector2(damageWidth, damageHeight));


        Gizmos.color = Color.cyan;

        Vector3 start = transform.position;
        Vector3 end = start + new Vector3((facingRight ? 1 : -1) * dashDistance, 0f, 0f);

        Gizmos.DrawLine(start, end);

        base.OnDrawGizmosSelected();
    }

}
