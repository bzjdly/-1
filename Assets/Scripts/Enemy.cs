using UnityEngine;
using System.Collections;
using System.Collections.Generic; // 需要使用 Dictionary

public class Enemy : MonoBehaviour
{
    [Header("基础设置")]
    [Tooltip("移动速度")]
    public float moveSpeed = 3f;
    [Tooltip("击退力度")]
    public float knockbackForce = 8f;

    [Header("玩家引用")]
    [Tooltip("玩家Transform，直接拖拽赋值")]
    public Transform player;

    [Header("受击反馈")]
    [Tooltip("受击闪烁时长（秒）")]
    public float hitFlashTime = 0.08f;
    [Tooltip("受击闪烁颜色")]
    public Color hitColor = Color.red;
    [Tooltip("受击粒子预制体（Unity ParticleSystem）")]
    public ParticleSystem hitParticlePrefab;
    [Tooltip("粒子发射速度")]
    public float hitParticleSpeed = 4f;
    [Tooltip("粒子发射数量")]
    public int hitParticleCount = 12;

    [Header("血量设置")]
    [Tooltip("最大血量")]
    public int maxHP = 3;
    [Tooltip("当前血量")]
    public int currentHP;

    [Header("击退控制")]
    [Tooltip("受击后停止移动时间（秒）")]
    public float hitStopTime = 0.18f;
    [Tooltip("击退倍率（影响击退力度）")]
    public float knockbackMultiplier = 1.0f;
    private float hitStopTimer = 0f;

    [Header("墙壁碰撞伤害")]
    [Tooltip("触发墙壁伤害的最低碰撞速度")]
    public float minWallImpactSpeed = 5f;
    [Tooltip("墙壁伤害倍率")]
    public float wallDamageMultiplier = 1f;

    [Header("敌人互撞伤害")]
    [Tooltip("触发敌人互撞伤害的最低相对速度")]
    public float minEnemyImpactSpeed = 4f;
    [Tooltip("敌人互撞伤害倍率")]
    public float enemyDamageMultiplier = 0.5f;

    [Header("击退衰减")]
    [Tooltip("击退期间线性阻力的倍率（越大衰减越快）")]
    public float knockbackDragMultiplier = 5f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isKnockedBack = false;
    private float originalDrag;

    // 新增：用于追踪子弹批次击中次数
    private Dictionary<int, int> bulletBatchHits = new Dictionary<int, int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            originalColor = sr.color;
        currentHP = maxHP;
        originalDrag = rb.drag;
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        Debug.Log($"[{name}] 速度={rb.velocity.magnitude:F2}, 击退中={isKnockedBack}");

        if (isKnockedBack)
        {
            if (hitStopTimer > 0f)
            {
                hitStopTimer -= Time.fixedDeltaTime;
            }

            if (hitStopTimer <= 0f && rb.velocity.magnitude < 2.0f)
            {
                EndKnockback();
            }
            return;
        }

        // 使用流场寻路移动
        if (EnemyManager.Instance != null)
        {
            Vector2 flowDirection = EnemyManager.Instance.GetDirectionToPlayer(transform.position);
            // 如果获取到有效的流场方向，则沿着该方向移动
            if (flowDirection != Vector2.zero)
            {
                // 使用平滑插值更新速度，减少在墙角处卡顿的可能性
                Vector2 targetVelocity = flowDirection.normalized * moveSpeed;
                rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 10f); // 10f 为插值速度，可调整
            } else
            {
                // 如果流场方向为零 (例如在障碍物内或无法到达玩家)，停止移动或保持当前速度
                // 这里简单设置为零，可以根据需要调整
                rb.velocity = Vector2.zero;
            }
        } else
        {
            // 如果 EnemyManager 实例不存在，则保持原有朝向玩家的移动逻辑作为备用或错误提示
            Debug.LogWarning("场景中没有 EnemyManager 实例，敌人将使用旧的追踪逻辑！");
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = dir * moveSpeed;
        }
    }

    public void OnHit(Vector2 hitDir, float hitPower, int damage = 1)
    {
        if (hitDir != Vector2.zero)
        {
             rb.AddForce(hitDir * hitPower * knockbackMultiplier, ForceMode2D.Impulse);
             StartKnockback();
        }
        
        if (!isFlashing && sr != null)
            StartCoroutine(HitFlash());
        currentHP -= damage;
        
        if (currentHP <= 0)
        {
            Destroy(gameObject);
            return;
        }
        if (hitParticlePrefab != null)
        {
            var ps = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
            var main = ps.main;
            main.startColor = hitColor;
            main.startSpeed = hitParticleSpeed;
            main.startLifetime = 0.4f;
            main.maxParticles = hitParticleCount;
            var shape = ps.shape;
            shape.angle = 15f;
            if (hitDir != Vector2.zero)
                ps.transform.rotation = Quaternion.LookRotation(Vector3.forward, -hitDir);
            else
                 ps.transform.rotation = Quaternion.LookRotation(Vector3.forward, Random.insideUnitCircle);

            ps.Emit(hitParticleCount);
            Destroy(ps.gameObject, main.startLifetime.constantMax + 0.1f);
        }
    }

    IEnumerator HitFlash()
    {
        isFlashing = true;
        sr.color = hitColor;
        yield return new WaitForSeconds(hitFlashTime);
        sr.color = originalColor;
        isFlashing = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            
            if (impactSpeed >= minWallImpactSpeed)
            {
                int wallDamage = Mathf.RoundToInt((impactSpeed - minWallImpactSpeed) * wallDamageMultiplier);
                wallDamage = Mathf.Max(1, wallDamage);
                
                OnHit(Vector2.zero, 0f, wallDamage);
                rb.velocity = Vector2.zero;

                if (isKnockedBack)
                {
                    EndKnockback();
                }
            }
            else if (isKnockedBack)
            {
                EndKnockback();
            }
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            
            if (impactSpeed >= minEnemyImpactSpeed)
            {
                int enemyDamage = Mathf.RoundToInt((impactSpeed - minEnemyImpactSpeed) * enemyDamageMultiplier);
                enemyDamage = Mathf.Max(1, enemyDamage);

                OnHit(Vector2.zero, 0f, enemyDamage);

                Enemy otherEnemy = collision.gameObject.GetComponent<Enemy>();
                if (otherEnemy != null)
                {
                    otherEnemy.OnHit(Vector2.zero, 0f, enemyDamage);
                }
            }
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
            }
            var player = collision.gameObject.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.OnHit(1);
            }
        }
    }

    void StartKnockback()
    {
        isKnockedBack = true;
        hitStopTimer = hitStopTime;
        rb.drag = originalDrag * knockbackDragMultiplier;
    }

    void EndKnockback()
    {
        isKnockedBack = false;
        hitStopTimer = 0f;
        rb.drag = originalDrag;
    }

    // 新增：处理来自子弹的批量击中信息
    public void OnBulletHitBatch(int batchID, int totalBulletsInBatch)
    {
        // 增加对应批次的击中计数
        if (!bulletBatchHits.ContainsKey(batchID))
        {
            bulletBatchHits[batchID] = 0;
        }
        bulletBatchHits[batchID]++;

        Debug.Log($"{gameObject.name} 收到批次 {batchID} 的子弹击中，当前计数: {bulletBatchHits[batchID]}/{totalBulletsInBatch}");

        // 检查是否达到总子弹数量，并且该批次还没有触发过时间停顿
        if (bulletBatchHits[batchID] >= totalBulletsInBatch)
        {
            Debug.Log($"{gameObject.name} 收到批次 {batchID} 全弹命中！触发时间停顿。");
            // 触发时间停顿
            if (HitFeedback.Instance != null)
            {
                // 可以在这里调整时间停顿的参数
                // HitFeedback.Instance.SlowMotion(0.15f, 0.1f); // 原来的直接调用

                // 启动协程延迟时间停顿，防止吞掉击退
                StartCoroutine(DelayedSlowMotion(0.02f, 0.15f, 0.1f)); // 延迟 0.02 秒，然后减速到 0.1 倍速，持续 0.15 秒
            }

            // 移除该批次的记录，避免重复触发
            bulletBatchHits.Remove(batchID);
        }
    }

    // 新增：延迟触发时间停顿的协程
    private IEnumerator DelayedSlowMotion(float delay, float slowdownDuration, float slowdownFactor)
    {
        yield return new WaitForSeconds(delay); // 等待指定的延迟时间

        if (HitFeedback.Instance != null)
        {
            HitFeedback.Instance.SlowMotion(slowdownDuration, slowdownFactor);
        }
    }
} 