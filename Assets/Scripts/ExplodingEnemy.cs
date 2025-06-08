using UnityEngine;

public class ExplodingEnemy : Enemy
{
    [Header("爆炸设置")]
    [Tooltip("爆炸半径")]
    public float explosionRadius = 3f;
    [Tooltip("爆炸伤害")]
    public int explosionDamage = 2;
    [Tooltip("爆炸击退力")]
    public float explosionForce = 10f;
    [Tooltip("爆炸粒子预制体")]
    public GameObject explosionEffectPrefab;

    protected override void StartDyingProcess(bool triggeredByExplosion = false)
    {
        Debug.Log($"ExplodingEnemy ({gameObject.name}) StartDyingProcess 被调用. triggeredByExplosion: {triggeredByExplosion}"); // 调试日志

        if (triggeredByExplosion)
        {
            // 如果是爆炸触发的死亡，则只执行基类的死亡流程，不再次爆炸
            base.StartDyingProcess(true);
            return;
        }

        // 如果不是爆炸触发的死亡，则正常爆炸
        Explode();
        base.StartDyingProcess(false); // 调用基类的死亡处理，等待动量归零再销毁
    }

    void Explode()
    {
        Debug.Log($"ExplodingEnemy ({gameObject.name}) Explode 被调用。"); // 调试日志

        // 播放爆炸粒子效果
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 获取爆炸范围内的所有Collider2D
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D hitCollider in colliders)
        {
            // 忽略自身
            if (hitCollider.gameObject == gameObject) continue;

            // 对玩家造成伤害
            if (hitCollider.CompareTag("Player"))
            {
                PlayerMovement playerMovement = hitCollider.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.TakeDamage(explosionDamage);
                    Vector2 knockbackDir = (hitCollider.transform.position - transform.position).normalized;
                    Rigidbody2D playerRb = hitCollider.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        playerRb.AddForce(knockbackDir * explosionForce, ForceMode2D.Impulse);
                    }
                }
            }
            // 对其他敌人造成伤害和击退
            else if (hitCollider.CompareTag("Enemy"))
            {
                Enemy otherEnemy = hitCollider.GetComponent<Enemy>();
                if (otherEnemy != null)
                {
                    otherEnemy.OnHit((hitCollider.transform.position - transform.position).normalized, explosionForce, explosionDamage, true); // 伤害来自爆炸
                }
            }
        }
    }
} 