
using UnityEngine;

public class targetDamage : MonoBehaviour
{
    public float health = 100f;

    public void hurtBaddies (float amount)
    {
        health -= amount;
        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
