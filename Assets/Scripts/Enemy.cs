using UnityEngine;

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

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            Debug.Log($"[{name}] 未设置玩家引用");
            return;
        }
        // 朝玩家方向移动
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = dir * moveSpeed;
        // 调试信息
        Debug.Log($"[{name}] 当前位置: {transform.position}, 玩家位置: {player.position}, 追踪方向: {dir}, 速度: {rb.velocity}");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 如果碰到玩家，给予玩家击退
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
} 