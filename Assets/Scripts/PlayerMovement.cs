using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; } // 单例模式

    [Header("移动设置")]
    [SerializeField]
    [Tooltip("移动速度")]
    private float moveSpeed = 5f;
    [SerializeField]
    [Tooltip("加速度")]
    private float acceleration = 20f; // 加速度
    [SerializeField]
    [Tooltip("减速度")]
    private float deceleration = 30f; // 减速度

    [Header("血量设置")]
    [Tooltip("最大血量")]
    public int maxHP = 5;
    [Tooltip("当前血量")]
    public int currentHP;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        // 让玩家始终面向鼠标（修正九十度偏差）
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 direction = (mouseWorldPos - transform.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void FixedUpdate()
    {
        Vector2 desiredVelocity = moveInput * moveSpeed; // 计算目标速度
        Vector2 velocityChange;

        // 根据是否有输入来决定使用加速度还是减速度
        if (moveInput.magnitude > 0.1f)
        {
            // 有输入时，向目标速度加速
            velocityChange = desiredVelocity - rb.velocity;
            rb.velocity += velocityChange * acceleration * Time.fixedDeltaTime;
        }
        else
        {
            // 没有输入时，减速到静止
            velocityChange = -rb.velocity;
            // 使用 MoveTowards 更方便实现减速到 0
            rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
        }

        // 限制速度不超过最大移动速度，防止通过反复输入叠加速度
        rb.velocity = Vector2.ClampMagnitude(rb.velocity, moveSpeed);
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHP <= 0) return; // 避免重复伤害已死亡的玩家

        currentHP -= damageAmount;
        Debug.Log($"玩家受到 {damageAmount} 点伤害，当前血量：{currentHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Debug.Log("玩家死亡！");
            // TODO: 在这里添加玩家死亡后的游戏逻辑，例如播放死亡动画、重新开始游戏等
        }
    }

    public void Heal(int healAmount)
    {
        if (currentHP >= maxHP) return; // 避免超过最大血量

        currentHP += healAmount;
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }
        Debug.Log($"玩家恢复 {healAmount} 点血量，当前血量：{currentHP}");
    }
} 