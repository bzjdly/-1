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

    [Header("打击感设置")]
    [Tooltip("开枪时屏幕缩放的目标Orthographic Size")]
    public float shootZoomSize = 7f;
    [Tooltip("开枪时屏幕缩放持续时间")]
    public float shootZoomDuration = 0.1f;
    [Tooltip("开枪时屏幕恢复正常大小所需时间")]
    public float shootZoomResetDuration = 0.2f;

    private float fireTimer = 0f;
    private static int shootBatchCounter = 0;
    private bool isZooming = false;

    void Update()
    {
        fireTimer += Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && fireTimer >= fireRate)
        {
            // 方向震动，主方向为枪口方向
            if (HitFeedback.Instance != null && firePoint != null)
            {
                Vector2 dir = firePoint.right;
                HitFeedback.Instance.DirectionalShake(dir);
                
                if (!isZooming)
                {
                    StartCoroutine(ShootZoomEffect());
                }
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

        shootBatchCounter++;
        int currentBatchID = shootBatchCounter;
        int totalBulletsInBatch = bulletCount;

        float startAngle = -spreadAngle * 0.5f;
        float angleStep = totalBulletsInBatch > 1 ? spreadAngle / (totalBulletsInBatch - 1) : 0f;
        float interval = totalBulletsInBatch > 1 ? spreadDuration / (totalBulletsInBatch - 1) : 0f;

        for (int i = 0; i < totalBulletsInBatch; i++)
        {
            float angleOffset = startAngle + angleStep * i;
            Quaternion rot = firePoint.rotation * Quaternion.Euler(0, 0, angleOffset);
            GameObject bulletGO = Instantiate(bulletPrefab, firePoint.position, rot);
            
            Bullet bullet = bulletGO.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.SetShootBatchInfo(currentBatchID, totalBulletsInBatch);
            } else
            {
                Debug.LogWarning($"子弹预制体 \"{bulletPrefab.name}\" 没有挂载 Bullet 脚本！");
            }

            Rigidbody2D rb = bulletGO.GetComponent<Rigidbody2D>();
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

    IEnumerator ShootZoomEffect()
    {
        isZooming = true;
        if (HitFeedback.Instance != null)
        {
            // 拉近屏幕
            HitFeedback.Instance.CameraZoom(shootZoomSize, shootZoomDuration);

            // 等待缩放持续时间
            yield return new WaitForSeconds(shootZoomDuration);

            // 恢复屏幕大小
            HitFeedback.Instance.ResetZoom();
        }
        // 等待恢复完成（或者更短的时间，避免缩放效果叠加）
        // 这里等待缩放持续时间和恢复持续时间的总和，确保下一次缩放不会太快触发
        yield return new WaitForSeconds(shootZoomDuration + shootZoomResetDuration); // 等待缩放和恢复动画完成
        isZooming = false;
    }
} 