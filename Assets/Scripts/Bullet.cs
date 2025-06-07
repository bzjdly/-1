using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("基础设置")]
    public float lifeTime = 2f;
    [Tooltip("速度衰减系数（每秒）")]
    public float speedDamping = 0.98f;
    [Tooltip("最低有效伤害速度")]
    public float minDamageSpeed = 3f;
    [Tooltip("撞墙反弹动能保留率")]
    public float wallBounceFactor = 0.6f;
    [Tooltip("击退基准系数")]
    public float knockbackBase = 1.2f;

    private Rigidbody2D rb;
    private float timer;

    private int shootBatchID; // 新增：射击批量ID
    private int totalBulletsInBatch; // 新增：当前批次总子弹数量

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        // 速度随时间衰减
        rb.velocity *= Mathf.Pow(speedDamping, Time.fixedDeltaTime * 60f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 撞墙反弹（假设墙体Layer为"Wall"）
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            // 反弹：速度沿法线反射，并损失动能
            Vector2 reflect = Vector2.Reflect(rb.velocity, collision.contacts[0].normal);
            rb.velocity = reflect * wallBounceFactor;
        }
        // 击中敌人
        if (collision.gameObject.CompareTag("Enemy"))
        {
            float speed = rb.velocity.magnitude;
            if (speed >= minDamageSpeed)
            {
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // 击退方向为子弹当前速度方向
                    Vector2 knockDir = rb.velocity.normalized;
                    float knockPower = speed * knockbackBase;
                    enemy.OnHit(knockDir, knockPower, 1);

                    // 调用敌人的新方法处理批量击中逻辑
                    enemy.OnBulletHitBatch(shootBatchID, totalBulletsInBatch, knockDir, knockPower);
                }
                // 这里可扩展造成伤害等逻辑
            }
            // 子弹击中敌人后销毁
            Destroy(gameObject);
        }
    }

    // 新增：设置射击批量信息
    public void SetShootBatchInfo(int batchID, int totalCount)
    {
        shootBatchID = batchID;
        totalBulletsInBatch = totalCount;
    }
} 