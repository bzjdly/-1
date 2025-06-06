using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 5f;
    [SerializeField]
    private float acceleration = 20f; // 加速度
    [SerializeField]
    private float deceleration = 30f; // 减速度

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();
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
} 