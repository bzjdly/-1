using UnityEngine;
using System.Collections;

public class HitFeedback : MonoBehaviour
{
    public static HitFeedback Instance;

    [Header("震动设置")]
    [Tooltip("震动持续时间（秒）")]
    public float shakeDuration = 0.2f;
    [Tooltip("震动强度")]
    public float shakeMagnitude = 0.2f;

    [Header("方向震动设置")]
    [Tooltip("方向震动主方向（单位向量）")]
    public Vector2 directionalShakeDir = Vector2.right;
    [Tooltip("主方向震动幅度")]
    public float directionalShakeMagnitude = 0.4f;
    [Tooltip("主方向震动持续时间（秒）")]
    public float directionalShakeDuration = 0.15f;
    [Tooltip("主方向震动的左右抖动幅度")]
    public float directionalShakeSideMagnitude = 0.08f;
    [Tooltip("主方向震动的左右抖动方向（单位向量）")]
    public Vector2 directionalShakeSideDir = Vector2.up;

    [Header("缩放设置")]
    [Tooltip("缩放目标值")]
    public float zoomTarget = 4.5f;
    [Tooltip("缩放速度")]
    public float zoomSpeed = 10f;

    [Header("游戏速度设置")]
    [Tooltip("慢动作时间缩放")]
    public float slowTimeScale = 0.5f;
    [Tooltip("慢动作持续时间（秒）")]
    public float slowDuration = 0.2f;

    private Camera mainCam;
    private float originalSize;
    private Vector3 originalPos;
    private Coroutine shakeCoroutine;
    private Coroutine zoomCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        mainCam = Camera.main;
        if (mainCam != null)
            originalSize = mainCam.orthographicSize;
    }

    // 摄像机震动
    public void CameraShake(float? duration = null, float? magnitude = null)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(DoShake(duration ?? shakeDuration, magnitude ?? shakeMagnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        originalPos = mainCam.transform.position;
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            mainCam.transform.position = originalPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        mainCam.transform.position = originalPos;
    }

    // 方向震动
    public void DirectionalShake(Vector2? dir = null, float? mainMag = null, float? sideMag = null, float? duration = null, Vector2? sideDir = null)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(DoDirectionalShake(
            dir ?? directionalShakeDir,
            mainMag ?? directionalShakeMagnitude,
            sideMag ?? directionalShakeSideMagnitude,
            duration ?? directionalShakeDuration,
            sideDir ?? directionalShakeSideDir
        ));
    }

    IEnumerator DoDirectionalShake(Vector2 dir, float mainMag, float sideMag, float duration, Vector2 sideDir)
    {
        float elapsed = 0f;
        originalPos = mainCam.transform.position;
        dir.Normalize();
        sideDir.Normalize();
        while (elapsed < duration)
        {
            float mainOffset = Mathf.Sin(elapsed / duration * Mathf.PI) * mainMag;
            float sideOffset = Random.Range(-1f, 1f) * sideMag;
            Vector2 offset = dir * mainOffset + sideDir * sideOffset;
            mainCam.transform.position = originalPos + new Vector3(offset.x, offset.y, 0);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        mainCam.transform.position = originalPos;
    }

    // 摄像机缩放
    public void CameraZoom(float target, float speed)
    {
        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(DoZoom(target, speed));
    }

    IEnumerator DoZoom(float target, float speed)
    {
        while (Mathf.Abs(mainCam.orthographicSize - target) > 0.01f)
        {
            mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, target, speed * Time.unscaledDeltaTime);
            yield return null;
        }
        mainCam.orthographicSize = target;
    }

    // 恢复摄像机缩放
    public void ResetZoom()
    {
        CameraZoom(originalSize, zoomSpeed);
    }

    // 游戏慢动作
    public void SlowMotion(float? scale = null, float? duration = null)
    {
        StartCoroutine(DoSlowMotion(scale ?? slowTimeScale, duration ?? slowDuration));
    }

    IEnumerator DoSlowMotion(float scale, float duration)
    {
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
} 