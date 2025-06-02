using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealDrop : MonoBehaviour
{
    public int healAmount = 50;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HealPlayer(collision.GetComponent<Character>());
            Destroy(gameObject);
        }
    }

    private void HealPlayer(Character targetPlayer)
    {
        if (targetPlayer != null)
        {
            targetPlayer.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
