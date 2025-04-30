using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionIgnoreCheck : MonoBehaviour
{
    private TheTarnishedWidow widow;

    private void Awake()
    {
        widow = GetComponentInParent<TheTarnishedWidow>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            widow.CollisionIgnore(true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            widow.CollisionIgnore(false);
        }
    }
}
