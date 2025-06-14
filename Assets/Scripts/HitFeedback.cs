using UnityEngine;
using System.Collections;
using UnityEngine.UI; // 新增引用

public class HitFeedback : MonoBehaviour
{
    public static HitFeedback Instance;

    [Header("震动设置")]
    [Tooltip("普通震动持续时间（秒）")]
    public float shakeDuration = 0.2f;
    [Tooltip("普通震动强度")]
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
    [Tooltip("开枪时屏幕缩放的目标正交相机大小 (Orthographic Size)")]
    public float zoomTarget = 4.5f;
    [Tooltip("开枪缩放插值速度")]
    public float zoomSpeed = 10f;

    [Header("游戏速度设置")]
    [Tooltip("慢动作时间缩放比例")]
    public float slowTimeScale = 0.5f;
    [Tooltip("慢动作持续时间（秒）")]
    public float slowDuration = 0.2f;

    [Header("摄像机跟随设置")]
    [Tooltip("跟随目标Transform（通常为玩家）")]
    public Transform followTarget;
    [Tooltip("摄像机跟随平滑速度")]
    public float followSmooth = 8f;
    [Tooltip("世界地图左边界X坐标")]
    public float worldLeft = -10f;
    [Tooltip("世界地图右边界X坐标")]
    public float worldRight = 10f;
    [Tooltip("世界地图下边界Y坐标")]
    public float worldBottom = -5f;
    [Tooltip("世界地图上边界Y坐标")]
    public float worldTop = 5f;

    [Header("满喷摄像机拉远设置")]
    [Tooltip("满喷时摄像机拉远过渡时间（秒）")]
    public float fullShotgunZoomTransitionDuration = 0.5f;
    [Tooltip("满喷时摄像机拉远后保持时间（秒，在慢动作期间计算）")]
    public float fullShotgunZoomHoldDuration = 0.3f;
    [Tooltip("满喷时摄像机拉远到的最小正交相机大小")]
    public float fullShotgunZoomMinSize = 8f;
    [Tooltip("满喷时摄像机拉远，基于玩家和敌人距离的额外视野填充")]
    public float fullShotgunZoomPadding = 2f;
    [Tooltip("满喷摄像机目标丢失后快速返回玩家的速度")]
    public float fullShotgunReturnSpeed = 15f;

    [Header("满喷击退碰撞震动设置")]
    [Tooltip("满喷击退敌人撞墙/人震动持续时间（秒）")]
    public float collisionShakeDuration = 0.1f;
    [Tooltip("满喷击退敌人撞墙/人震动强度")]
    public float collisionShakeMagnitude = 0.15f;

    [Header("玩家受击反馈")] // 新增：玩家受击反馈分组
    [Tooltip("玩家受击时摄像机震动持续时间（秒）")]
    public float playerHitShakeDuration = 0.3f;
    [Tooltip("玩家受击时摄像机震动强度")]
    public float playerHitShakeMagnitude = 0.3f;
    [Tooltip("屏幕边缘泛红Image引用（需手动创建并设置为全屏）")]
    public Image playerHitVignetteImage;
    [Tooltip("屏幕边缘泛红颜色")]
    public Color playerHitVignetteColor = new Color(1f, 0f, 0f, 0.5f); // 默认红色，半透明
    [Tooltip("屏幕边缘泛红淡入时间（秒）")]
    public float playerHitVignetteFadeInDuration = 0.1f;
    [Tooltip("屏幕边缘泛红淡出时间（秒）")]
    public float playerHitVignetteFadeOutDuration = 0.5f;

    private Camera mainCam;
    private float originalSize;
    private Vector3 originalPos;
    private Coroutine shakeCoroutine;
    private Coroutine zoomCoroutine;
    private Coroutine focusCoroutine;
    private Coroutine vignetteCoroutine; // 新增：用于控制泛红特效的协程

    private Vector3 _currentShakeOffset = Vector3.zero; // 新增：当前震动偏移量

    // 是否正在聚焦中点 (现在更多是表示是否处于满喷摄像机特殊状态)
    private bool isFocusingOnMidpoint = false;
    // 中点聚焦的另一个目标（敌人）
    private Transform midpointFocusTarget;
    // 聚焦前的原始跟随位置
    private Vector3 originalFollowPos;
    private float originalOrthographicSize;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        mainCam = Camera.main;
        if (mainCam != null)
            originalSize = mainCam.orthographicSize;

        // 初始化屏幕边缘泛红Image为透明
        if (playerHitVignetteImage != null)
        {
            playerHitVignetteImage.color = Color.clear;
        }
    }

    void LateUpdate()
    {
        if (!isFocusingOnMidpoint)
        {
            CameraFollowWithBounds();
        }
    }

    void CameraFollowWithBounds()
    {
        if (followTarget == null || mainCam == null) return;
        Vector3 targetPos = followTarget.position;
        float camHeight = mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        float minX = worldLeft + camWidth;
        float maxX = worldRight - camWidth;
        float minY = worldBottom + camHeight;
        float maxY = worldTop - camHeight;
        float x = Mathf.Clamp(targetPos.x, minX, maxX);
        float y = Mathf.Clamp(targetPos.y, minY, maxY);
        Vector3 desired = new Vector3(x, y, mainCam.transform.position.z);

        // 应用震动偏移量
        mainCam.transform.position = desired + _currentShakeOffset; // 将震动偏移量添加到计算出的目标位置
    }

    public void CameraShake(float? duration = null, float? magnitude = null)
    {
        Debug.Log("CameraShake 方法被调用。"); // 调试日志
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(DoShakeInternal(duration ?? shakeDuration, magnitude ?? shakeMagnitude));
    }

    IEnumerator DoShakeInternal(float duration, float magnitude)
    {
        float elapsed = 0f;
        Vector3 startShakeOffset = _currentShakeOffset; // 记录开始时的偏移量，以防多个震动叠加

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            // 直接更新震动偏移量，而不是相机位置
            _currentShakeOffset = Vector3.Lerp(startShakeOffset, new Vector3(offsetX, offsetY, 0), elapsed / duration);
            
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        // 震动结束，平滑过渡回零偏移
        float smoothReturnElapsed = 0f;
        Vector3 returnStartOffset = _currentShakeOffset;
        float returnDuration = 0.2f; // 平滑返回时间

        while (smoothReturnElapsed < returnDuration)
        {
            _currentShakeOffset = Vector3.Lerp(returnStartOffset, Vector3.zero, smoothReturnElapsed / returnDuration);
            smoothReturnElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _currentShakeOffset = Vector3.zero; // 确保最终为零
    }

    public void DirectionalShake(Vector2? dir = null, float? mainMag = null, float? sideMag = null, float? duration = null, Vector2? sideDir = null)
    {
        Debug.Log("DirectionalShake 方法被调用。"); // 调试日志
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(DoDirectionalShakeInternal(
            dir ?? directionalShakeDir,
            mainMag ?? directionalShakeMagnitude,
            sideMag ?? directionalShakeSideMagnitude,
            duration ?? directionalShakeDuration,
            sideDir ?? directionalShakeSideDir
        ));
    }

    IEnumerator DoDirectionalShakeInternal(Vector2 dir, float mainMag, float sideMag, float duration, Vector2 sideDir)
    {
        float elapsed = 0f;
        Vector3 startShakeOffset = _currentShakeOffset; // 记录开始时的偏移量

        dir.Normalize();
        sideDir.Normalize();

        while (elapsed < duration)
        {
            float mainOffset = Mathf.Sin(elapsed / duration * Mathf.PI) * mainMag;
            float sideOffset = Random.Range(-1f, 1f) * sideMag;
            Vector2 offset = dir * mainOffset + sideDir * sideOffset;
            
            // 直接更新震动偏移量
            _currentShakeOffset = Vector3.Lerp(startShakeOffset, new Vector3(offset.x, offset.y, 0), elapsed / duration);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        // 震动结束，平滑过渡回零偏移
        float smoothReturnElapsed = 0f;
        Vector3 returnStartOffset = _currentShakeOffset;
        float returnDuration = 0.2f; // 平滑返回时间

        while (smoothReturnElapsed < returnDuration)
        {
            _currentShakeOffset = Vector3.Lerp(returnStartOffset, Vector3.zero, smoothReturnElapsed / returnDuration);
            smoothReturnElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _currentShakeOffset = Vector3.zero; // 确保最终为零
    }

    public void CameraZoom(float target, float speed)
    {
        Debug.Log($"CameraZoom 方法被调用，目标大小: {target}, 速度: {speed}。"); // 调试日志
        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);
        // 使用不受时间缩放影响的插值速度
        zoomCoroutine = StartCoroutine(DoZoom(target, speed * Time.unscaledDeltaTime));
    }

    IEnumerator DoZoom(float target, float speed)
    {
        while (Mathf.Abs(mainCam.orthographicSize - target) > 0.01f)
        {
            // 使用不受时间缩放影响的插值
            mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, target, speed);
            yield return null;
        }
        mainCam.orthographicSize = target;
    }

    public void ResetZoom()
    {
        Debug.Log("ResetZoom 方法被调用。"); // 调试日志
        CameraZoom(originalSize, zoomSpeed);
    }

    public void SlowMotion(float? scale = null, float? duration = null)
    {
        Debug.Log($"SlowMotion 方法被调用，时间缩放: {scale}, 持续时间: {duration}。"); // 调试日志
        StartCoroutine(DoSlowMotion(scale ?? slowTimeScale, duration ?? slowDuration));
    }

    IEnumerator DoSlowMotion(float scale, float duration)
    {
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    // 满喷时触发摄像机拉远并聚焦玩家和目标
    public void TriggerFullShotgunCameraEffect(Transform target)
    {
        if (followTarget == null || target == null || mainCam == null) return;
        if (focusCoroutine != null) StopCoroutine(focusCoroutine);

        midpointFocusTarget = target;
        isFocusingOnMidpoint = true;
        originalFollowPos = mainCam.transform.position; // 记录当前相机位置
        originalOrthographicSize = mainCam.orthographicSize; // 记录当前相机大小

        // 使用 Time.unscaledDeltaTime 进行平滑过渡，不受慢动作影响
        focusCoroutine = StartCoroutine(DoFullShotgunCameraEffect(target));
    }

    // 满喷时摄像机拉远效果协程
    private IEnumerator DoFullShotgunCameraEffect(Transform target)
    {
        float elapsed = 0f;
        Vector3 startPos = mainCam.transform.position;
        float startSize = mainCam.orthographicSize;

        // 计算目标位置（玩家和敌人中点）和目标相机大小
        Vector3 targetMidpoint = Vector3.Lerp(followTarget.position, target.position, 0.5f);
        float distance = Vector3.Distance(followTarget.position, target.position);
        // 计算需要覆盖玩家和敌人以及填充所需的最小视野大小
        float requiredSize = distance * 0.5f / mainCam.aspect + fullShotgunZoomPadding; // 考虑宽高比
        float targetSize = Mathf.Max(fullShotgunZoomMinSize, requiredSize);

        // 平滑过渡到目标位置和大小
        while (elapsed < fullShotgunZoomTransitionDuration)
        {
             // 新增：检查目标是否被销毁
            if (target == null)
            {
                Debug.LogWarning("满喷摄像机目标已被销毁 (过渡中)。");
                Time.timeScale = 1f; // 立即停止慢动作
                StartCoroutine(QuickReturnToPlayer()); // 启动快速返回协程
                yield break; // 提前结束当前协程
            }

            // 插值位置和大小，使用非缩放时间
            Vector3 currentTargetPos = Vector3.Lerp(followTarget.position, target.position, 0.5f); // 实时计算中点
            mainCam.transform.position = Vector3.Lerp(startPos, new Vector3(currentTargetPos.x, currentTargetPos.y, mainCam.transform.position.z), elapsed / fullShotgunZoomTransitionDuration);
            mainCam.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / fullShotgunZoomTransitionDuration);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 确保到达目标位置和大小
        Vector3 finalTargetPos = Vector3.Lerp(followTarget.position, target.position, 0.5f);
        mainCam.transform.position = new Vector3(finalTargetPos.x, finalTargetPos.y, mainCam.transform.position.z);
        mainCam.orthographicSize = targetSize;

        // 在目标位置和大小保持一段时间 (使用缩放时间)
        yield return new WaitForSeconds(fullShotgunZoomHoldDuration);

        // 平滑过渡回玩家跟随和原始相机大小 (正常结束)
        elapsed = 0f;
        startPos = mainCam.transform.position; // 使用保持后的相机实际位置作为起点
        startSize = mainCam.orthographicSize; // 使用保持后的相机实际大小作为起点
        Vector3 returnStartPos = startPos; // 返回动画的起始位置
        float returnStartSize = startSize; // 返回动画的起始大小

        // 检查目标是否在保持阶段被销毁
        if (target == null)
        {
            Debug.LogWarning("满喷摄像机目标已被销毁 (保持中)。");
            Time.timeScale = 1f; // 立即停止慢动作
            StartCoroutine(QuickReturnToPlayer()); // 启动快速返回协程
            yield break; // 提前结束当前协程
        }

        while (elapsed < fullShotgunZoomTransitionDuration)
        {
             // 新增：检查目标是否被销毁 (返回过程中)
            if (target == null)
            {
                Debug.LogWarning("满喷摄像机目标已被销毁 (返回中)。");
                Time.timeScale = 1f; // 立即停止慢动作
                StartCoroutine(QuickReturnToPlayer()); // 启动快速返回协程
                yield break; // 提前结束当前协程
            }

             // 插值回原始跟随位置和大小，使用非缩放时间
            mainCam.transform.position = Vector3.Lerp(returnStartPos, new Vector3(originalFollowPos.x, originalFollowPos.y, mainCam.transform.position.z), elapsed / fullShotgunZoomTransitionDuration);
            mainCam.orthographicSize = Mathf.Lerp(returnStartSize, originalOrthographicSize, elapsed / fullShotgunZoomTransitionDuration);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 确保回到原始跟随位置和大小附近，然后让 LateUpdate 的正常跟随逻辑接管
         mainCam.transform.position = new Vector3(originalFollowPos.x, originalFollowPos.y, mainCam.transform.position.z);
         mainCam.orthographicSize = originalOrthographicSize;

        isFocusingOnMidpoint = false;
        midpointFocusTarget = null;
        focusCoroutine = null;
    }

    // 新增：目标丢失后快速返回玩家的协程
    private IEnumerator QuickReturnToPlayer()
    {
        // 立即停止慢动作 (在调用此协程前已经停止，这里再次确保)
        Time.timeScale = 1f;

        float elapsed = 0f;
        Vector3 startPos = mainCam.transform.position;
        float startSize = mainCam.orthographicSize;
        // 计算目标位置 (玩家当前位置，考虑边界)
        Vector3 targetPos = ApplyBounds(followTarget.position);
        targetPos.z = mainCam.transform.position.z; // 保持相机Z轴不变

        // 计算过渡时间 (基于返回速度和距离/大小)
        float positionDistance = Vector3.Distance(startPos, targetPos);
        float sizeDistance = Mathf.Abs(startSize - originalOrthographicSize);
        // 取最大距离来计算过渡时间，确保位置和大小都有足够时间平滑
        float transitionDuration = Mathf.Max(positionDistance, sizeDistance) / fullShotgunReturnSpeed;
        transitionDuration = Mathf.Max(0.1f, transitionDuration); // 确保过渡时间不会太短

        while (elapsed < transitionDuration)
        {
            // 插值位置和大小，使用非缩放时间
            mainCam.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / transitionDuration);
            mainCam.orthographicSize = Mathf.Lerp(startSize, originalOrthographicSize, elapsed / transitionDuration);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 确保到达目标位置和大小
         mainCam.transform.position = targetPos;
         mainCam.orthographicSize = originalOrthographicSize;

        isFocusingOnMidpoint = false;
        midpointFocusTarget = null;
        focusCoroutine = null;
    }

    // 新增：触发满喷击退敌人的碰撞震动
    public void TriggerCollisionShake()
    {
        // 调用普通的摄像机震动，使用专门的碰撞震动参数
        CameraShake(collisionShakeDuration, collisionShakeMagnitude);
    }

    // helper function to apply bounds
    private Vector3 ApplyBounds(Vector3 position)
    {
         if (mainCam == null) return position; // Safety check

         float camHeight = mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        float minX = worldLeft + camWidth;
        float maxX = worldRight - camWidth;
        float minY = worldBottom + camHeight;
        float maxY = worldTop - camHeight;
        float x = Mathf.Clamp(position.x, minX, maxX);
        float y = Mathf.Clamp(position.y, minY, maxY);
        return new Vector3(x, y, position.z);
    }

    // 新增：触发玩家受击反馈（震动和屏幕泛红）
    public void TriggerPlayerHitFeedback()
    {
        Debug.Log("TriggerPlayerHitFeedback 方法被调用。"); // 调试日志
        CameraShake(playerHitShakeDuration, playerHitShakeMagnitude); // 触发震动
        if (playerHitVignetteImage != null)
        {
            if (vignetteCoroutine != null)
            {
                StopCoroutine(vignetteCoroutine);
            }
            vignetteCoroutine = StartCoroutine(DoPlayerHitVignette()); // 触发泛红特效
        }
    }

    // 新增：处理屏幕边缘泛红效果的协程
    private IEnumerator DoPlayerHitVignette()
    {
        Color startColor = playerHitVignetteImage.color;
        Color targetColor = playerHitVignetteColor;

        // 淡入
        float elapsed = 0f;
        while (elapsed < playerHitVignetteFadeInDuration)
        {
            playerHitVignetteImage.color = Color.Lerp(startColor, targetColor, elapsed / playerHitVignetteFadeInDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        playerHitVignetteImage.color = targetColor; // 确保完全淡入

        // 淡出
        elapsed = 0f;
        startColor = playerHitVignetteImage.color; // 从当前颜色开始淡出
        while (elapsed < playerHitVignetteFadeOutDuration)
        {
            playerHitVignetteImage.color = Color.Lerp(startColor, Color.clear, elapsed / playerHitVignetteFadeOutDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        playerHitVignetteImage.color = Color.clear; // 确保完全透明
        vignetteCoroutine = null;
    }
} 