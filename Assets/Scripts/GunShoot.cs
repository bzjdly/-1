using UnityEngine;

public class GunShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float fireRate = 0.5f;
    public Transform firePoint;
    public int bulletCount = 6; // 一次发射的子弹数
    public float spreadAngle = 15f; // 总扩散角度（度）
    public float speedVariance = 2f; // 子弹速度偏差

    private float fireTimer = 0f;

    void Update()
    {
        fireTimer += Time.deltaTime;
        if (Input.GetMouseButton(0) && fireTimer >= fireRate)
        {
            Debug.Log($"[GunShoot] Shoot! bulletCount={bulletCount}, spreadAngle={spreadAngle}, speedVariance={speedVariance}");
            Shoot();
            fireTimer = 0f;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("[GunShoot] bulletPrefab 或 firePoint 未设置！");
            return;
        }
        float startAngle = -spreadAngle * 0.5f;
        float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;
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
                Debug.Log($"[GunShoot] Bullet {i}: pos={firePoint.position}, angle={rot.eulerAngles.z}, velocity={velocity}");
            }
            else
            {
                Debug.LogError($"[GunShoot] Bullet {i} 没有 Rigidbody2D 组件！");
            }
        }
    }
} 