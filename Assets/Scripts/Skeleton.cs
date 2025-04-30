using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : Enemy
{
    private int attackPhase = 0;
    private float lastAttackTime = 0f;
    public float attackSpeed = 0.5f;
    protected override void AttackPlayer()
    {
        if (Time.time - lastAttackTime > attackSpeed)
        {
            attackPhase = (attackPhase % 2) + 1;
            animator.SetTrigger($"Attack{attackPhase}");
            lastAttackTime = Time.time;
        }
    }
    protected override void Die()
    {
        animator.SetTrigger("Die");
    }
}
