using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockSweeper : Assassin
{
    protected override void AttackPlayer()
    {
        if (isDashing || Time.time - lastAttackTime < attackSpeed) return;

        attackPhase = (attackPhase % 2) + 1;
        animator.SetTrigger($"Attack{attackPhase}");
        canMove = false;
        lastAttackTime = Time.time;
    }
}
