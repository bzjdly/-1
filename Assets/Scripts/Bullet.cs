using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 2f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 这里可以扩展击中敌人等逻辑
        Destroy(gameObject);
    }
} 