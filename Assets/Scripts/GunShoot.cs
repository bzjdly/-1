using UnityEngine;
using System.Collections;

public class GunShoot : MonoBehaviour
{
    [Header("弹药设置")]
    [Tooltip("子弹预制体")]
    public GameObject bulletPrefab;
    [Tooltip("基础子弹速度")]
    public float bulletSpeed = 20f;
    [Tooltip("每次发射的子弹数量")]
    public int bulletCount = 6;
    [Tooltip("总扩散角度（度）")]
    public float spreadAngle = 15f;
    [Tooltip("子弹速度偏差")]
    public float speedVariance = 2f;
    [Tooltip("所有子弹发射完毕所需时间（秒）")]
    public float spreadDuration = 0.1f;

    [Header("射击设置")]
    [Tooltip("射速（秒/发）")]
    public float fireRate = 0.5f;
    [Tooltip("子弹发射点")]
    public Transform firePoint;

    private float fireTimer = 0f;
    private static int shootBatchCounter = 0; // 用于生成唯一的批量ID

    void Update()
    {
        fireTimer += Time.deltaTime;
        if (Input.GetMouseButton(0) && fireTimer >= fireRate)
        {
            // 方向震动，主方向为枪口方向
            if (HitFeedback.Instance != null && firePoint != null)
            {
                Vector2 dir = firePoint.right;
                HitFeedback.Instance.DirectionalShake(dir);
            }
            StartCoroutine(ShootSpread());
            fireTimer = 0f;
        }
    }

    IEnumerator ShootSpread()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            yield break;
        }

        shootBatchCounter++; // 为本次射击生成一个新的批量ID
        int currentBatchID = shootBatchCounter;
        int totalBulletsInBatch = bulletCount; // 本批次总子弹数量

        float startAngle = -spreadAngle * 0.5f;
        float angleStep = totalBulletsInBatch > 1 ? spreadAngle / (totalBulletsInBatch - 1) : 0f;
        float interval = totalBulletsInBatch > 1 ? spreadDuration / (totalBulletsInBatch - 1) : 0f;

        for (int i = 0; i < totalBulletsInBatch; i++)
        {
            float angleOffset = startAngle + angleStep * i;
            Quaternion rot = firePoint.rotation * Quaternion.Euler(0, 0, angleOffset);
            GameObject bulletGO = Instantiate(bulletPrefab, firePoint.position, rot); // 修改变量名方便获取组件
            
            // 获取 Bullet 组件并设置批量ID和总子弹数量
            Bullet bullet = bulletGO.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.SetShootBatchInfo(currentBatchID, totalBulletsInBatch);
            } else
            {
                Debug.LogWarning($"子弹预制体 \"{bulletPrefab.name}\" 没有挂载 Bullet 脚本！");
            }

            Rigidbody2D rb = bulletGO.GetComponent<Rigidbody2D>(); // 使用新的变量名
            if (rb != null)
            {
                float speedOffset = Random.Range(-speedVariance, speedVariance);
                Vector2 velocity = rot * Vector2.right * (bulletSpeed + speedOffset);
                rb.velocity = velocity;
            }
            if (i < totalBulletsInBatch - 1)
                yield return new WaitForSeconds(interval);
        }
    }
} 