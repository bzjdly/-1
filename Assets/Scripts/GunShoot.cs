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
        float startAngle = -spreadAngle * 0.5f;
        float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;
        float interval = bulletCount > 1 ? spreadDuration / (bulletCount - 1) : 0f;
        for (int i = 0; i < bulletCount; i++)
        {
            float angleOffset = startAngle + angleStep * i;
            Quaternion rot = firePoint.rotation * Quaternion.Euler(0, 0, angleOffset);
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rot);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float speedOffset = Random.Range(-speedVariance, speedVariance);
                Vector2 velocity = rot * Vector2.right * (bulletSpeed + speedOffset);
                rb.velocity = velocity;
            }
            if (i < bulletCount - 1)
                yield return new WaitForSeconds(interval);
        }
    }
} 