using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using static UnityEngine.UI.Image;

public class MageAttack : MonoBehaviour
{
    public int damage = 20;
    public float knockbackForceX = 10f;
    public float knockbackForceY = 10f;
    public float knockbackDuration = 0.6f;
    public float damageWidth = 0.5f;
    public float damageHeight = 0.5f;
    public LayerMask playerLayer;
    public Transform middlePosition;

    private void Damage()
    {
        Vector2 size = new(damageWidth, damageHeight);
        Vector2 origin = middlePosition != null ? (Vector2)middlePosition.position : (Vector2)transform.position;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, Vector2.right, 0.1f, playerLayer);

        foreach (var hit in hits)
        {
            var player = hit.collider.GetComponent<Character>();
            if (player != null)
            {
                player.TakeDamage(damage, transform, knockbackForceX, knockbackForceY, knockbackDuration);
            }
        }
    }

    private void DestroyObject()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (middlePosition == null) return;

        Gizmos.color = Color.yellow;

        Vector2 origin = middlePosition.position;
        Vector2 size = new Vector2(damageWidth, damageHeight);
        Vector2 direction = Vector2.up;
        float distance = 0.1f;

        Vector2 castCenter = origin + direction.normalized * distance * 0.5f;

        Gizmos.DrawWireCube(castCenter, size);
    }

}
