using UnityEngine;
using System.Collections;
using System.Collections.Generic; // 需要使用 Dictionary

public class Enemy : MonoBehaviour
{
    [Header("基础设置")]
    [Tooltip("移动速度")]
    public float moveSpeed = 3f;
    [Tooltip("击退力度（基础值）")]
    public float knockbackForce = 8f;
    [Tooltip("对玩家造成的伤害")]
    public int playerDamage = 1; // 新增：对玩家造成的伤害值

    [Header("玩家引用")]
    [Tooltip("玩家Transform，由EnemyManager自动赋值")]
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
    [Tooltip("普通击退倍率（影响OnHit方法中的击退力度）")]
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
    [Tooltip("敌人死亡后等待动量归零的最小速度阈值")]
    public float minVelocityForDestroy = 0.1f;

    [Header("满喷打击反馈")]
    [Tooltip("满喷时慢动作持续时间（秒）")]
    public float fullShotgunSlowdownDuration = 0.15f;
    [Tooltip("满喷时慢动作因子 (1.0为正常速度)")]
    public float fullShotgunSlowdownFactor = 0.3f;
    [Tooltip("满喷时应用击退前的延迟（秒，在慢动作开始后计算）")]
    public float fullShotgunKnockbackDelay = 0.02f;
    [Tooltip("满喷时固定的击退力量")]
    public float fullShotgunFixedKnockbackForce = 15.0f;

    [Header("调试设置")]
    [Tooltip("满喷命中时敌人显示的颜色（仅调试用）")]
    public Color fullShotgunDebugColor = Color.blue;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isKnockedBack = false;
    private float originalDrag;

    // 用于追踪子弹批次击中次数
    private Dictionary<int, int> bulletBatchHits = new Dictionary<int, int>();
    // 用于存储满喷时的击退信息
    private Vector2 pendingKnockbackDir;
    private float pendingKnockbackPower;
    // 用于存储满喷时的原始颜色，以便恢复
    private Color fullShotgunOriginalColor;
    // 新增：是否处于满喷击退状态
    private bool isFullShotgunKnockedBack = false;

    // 新增：标记敌人是否进入死亡过程
    private bool isDying = false;

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
        // 如果敌人正在死亡过程中，则只处理其动量归零逻辑
        if (isDying)
        {
            // 确保敌人不再进行寻路或被击退处理
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * 5f); // 确保减速，即使没有外部阻力

            if (rb.velocity.magnitude <= minVelocityForDestroy)
            {
                Destroy(gameObject); // 动量归零后销毁
            }
            return; // 阻止后续的移动和击退逻辑
        }

        if (player == null)
            return;

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
            // Debug.LogWarning("场景中没有 EnemyManager 实例，敌人将使用旧的追踪逻辑！");
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = dir * moveSpeed;
        }
    }

    public void OnHit(Vector2 hitDir, float hitPower, int damage = 1, bool isFromExplosion = false)
    {
        if (isDying) return; // 已死亡的敌人不再受伤害

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
            StartDyingProcess(isFromExplosion); // 触发死亡过程，并传递是否来源于爆炸
            // return; // 不再立即返回，让粒子效果等逻辑继续执行
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
            
            // 新增：满喷击退敌人撞墙时触发震动
            if (isFullShotgunKnockedBack && HitFeedback.Instance != null)
            {
                HitFeedback.Instance.TriggerCollisionShake();
            }

            if (impactSpeed >= minWallImpactSpeed)
            {
                int wallDamage = Mathf.RoundToInt((impactSpeed - minWallImpactSpeed) * wallDamageMultiplier);
                wallDamage = Mathf.Max(1, wallDamage);
                
                OnHit(Vector2.zero, 0f, wallDamage, false); // 墙壁伤害不是来自爆炸
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
            
            // 新增：满喷击退敌人撞其他敌人时触发震动
            if (isFullShotgunKnockedBack && HitFeedback.Instance != null)
            {
                HitFeedback.Instance.TriggerCollisionShake();
            }

            if (impactSpeed >= minEnemyImpactSpeed)
            {
                int enemyDamage = Mathf.RoundToInt((impactSpeed - minEnemyImpactSpeed) * enemyDamageMultiplier);
                enemyDamage = Mathf.Max(1, enemyDamage);

                OnHit(Vector2.zero, 0f, enemyDamage, false); // 敌人互撞不是来自爆炸

                Enemy otherEnemy = collision.gameObject.GetComponent<Enemy>();
                if (otherEnemy != null)
                {
                    otherEnemy.OnHit(Vector2.zero, 0f, enemyDamage, false); // 敌人互撞不是来自爆炸
                }
            }
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            // 检查玩家是否已死亡，避免重复伤害
            if (PlayerMovement.Instance != null && PlayerMovement.Instance.currentHP <= 0)
            {
                return;
            }

            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                // 对玩家造成伤害
                if (PlayerMovement.Instance != null)
                {
                    PlayerMovement.Instance.TakeDamage(playerDamage);
                }

                // 对玩家施加击退，力量由敌人的 knockbackForce 决定
                playerRb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    void StartKnockback()
    {
        isKnockedBack = true;
        hitStopTimer = hitStopTime;
        if (isFullShotgunKnockedBack) // 如果是满喷击退，减少速度衰减
        {
            rb.drag = 0.01f; // 几乎无阻力，模拟无速度衰减
        }
        else
        {
            rb.drag = originalDrag * knockbackDragMultiplier; // 普通击退应用阻力
        }
    }

    void EndKnockback()
    {
        isKnockedBack = false;
        rb.drag = originalDrag;
        hitStopTimer = 0f;
        // 确保在击退结束时重置满喷击退标志
        isFullShotgunKnockedBack = false;
    }

    // 新增：处理来自子弹的批量击中信息 (添加 hitDir 和 hitPower 参数)
    public void OnBulletHitBatch(int batchID, int totalBulletsInBatch, Vector2 hitDir, float hitPower)
    {
        // Debug.Log($"{gameObject.name} 收到批次 {batchID} 的子弹击中，当前计数: {bulletBatchHits[batchID]}/{totalBulletsInBatch}"); // 调试日志
        // 增加对应批次的击中计数
        if (!bulletBatchHits.ContainsKey(batchID))
        {
            bulletBatchHits[batchID] = 0;
        }
        bulletBatchHits[batchID]++;

        // 检查是否达到全弹命中
        if (bulletBatchHits[batchID] == totalBulletsInBatch)
        {
            // Debug.Log($"敌人 {gameObject.name} 全弹命中! BatchID: {batchID}"); // 调试日志
            // 存储击退信息，延迟应用
            pendingKnockbackDir = hitDir;
            pendingKnockbackPower = fullShotgunFixedKnockbackForce; // 确保使用固定击退力量

            // 触发慢动作
            HitFeedback.Instance?.SlowMotion(fullShotgunSlowdownFactor, fullShotgunSlowdownDuration);

            // 触发摄像机聚焦到玩家和该敌人的中点
            HitFeedback.Instance?.TriggerFullShotgunCameraEffect(transform);

            // 新增：满喷命中时临时变色
            if (sr != null)
            {
                 fullShotgunOriginalColor = sr.color; // 存储原始颜色
                 sr.color = fullShotgunDebugColor;
            }

            // 启动协程延迟应用击退
            StartCoroutine(ApplyFullShotgunKnockback(fullShotgunKnockbackDelay));

            // 可选：清理该批次的记录，避免重复触发
            // bulletBatchHits.Remove(batchID);
        }
    }

    // 新增：延迟应用满喷击退的协程
    private IEnumerator ApplyFullShotgunKnockback(float delay)
    {
        // Debug.Log($"<color=orange>Full Shotgun Knockback Applied:</color> Dir={pendingKnockbackDir.normalized}, Force={fullShotgunFixedKnockbackForce}"); // 调试日志
        // 新增：存储满喷时的原始颜色用于调试显示
        fullShotgunOriginalColor = sr.color;

        yield return new WaitForSeconds(delay); // 等待一小段延迟

        // 确保目标仍然存在（尽管在 OnBulletHitBatch 中已经检查过，这里再次保险）
        if (gameObject == null) yield break;

        // 施加固定力量的击退冲量
        // 方向使用存储的 pendingKnockbackDir (已归一化)，力量使用 fullShotgunFixedKnockbackForce
        rb.AddForce(pendingKnockbackDir.normalized * fullShotgunFixedKnockbackForce, ForceMode2D.Impulse);

        // 启动普通击退流程 (处理击退停顿和阻力)
        StartKnockback();

        // 新增：设置满喷击退标志
        isFullShotgunKnockedBack = true;

        // 在Inspector中设置调试颜色（可选）
        if (sr != null) sr.color = fullShotgunDebugColor;

        // 触发慢动作 (如果 HitFeedback 存在)
        HitFeedback.Instance?.SlowMotion(fullShotgunSlowdownFactor, fullShotgunSlowdownDuration);

        // 触发摄像机拉远聚焦 (如果 HitFeedback 存在)
        HitFeedback.Instance?.TriggerFullShotgunCameraEffect(transform);


        // 等待满喷慢动作持续时间结束后恢复颜色
        // 注意：这里的等待时间应该匹配慢动作的持续时间，确保调试颜色显示足够长
        yield return new WaitForSeconds(fullShotgunSlowdownDuration); // 使用慢动作持续时间

         // 确保目标仍然存在
        if (gameObject == null) yield break;

        // 恢复原始颜色
        if (sr != null) sr.color = fullShotgunOriginalColor;

        // 注意：击退状态的结束和 isFullShotgunKnockedBack 标志的重置由 EndKnockback() 方法处理
    }

    // 新增：处理敌人死亡过程的方法
    protected virtual void StartDyingProcess(bool triggeredByExplosion = false)
    {
        isDying = true;
        // 可选：改变颜色或透明度以视觉上表示死亡
        if (sr != null)
        {
            sr.color = Color.grey; // 例如，变为灰色
            // 或者 sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f); // 半透明
        }

        // 停止闪烁协程（如果正在运行）
        if (isFlashing)
        {
            StopCoroutine("HitFlash"); // 假设 HitFlash 协程的名称是 "HitFlash"
            sr.color = Color.grey; // 确保在停止闪烁后仍然是死亡颜色
            isFlashing = false; // 重置闪烁状态
        }

        // 结束任何击退状态，确保不再进行击退逻辑
        EndKnockback();
    }
} 