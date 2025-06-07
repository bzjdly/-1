using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("血条Slider组件")]
    public Slider healthSlider;
    [Tooltip("玩家移动脚本引用 (PlayerMovement)")]
    public PlayerMovement playerMovement;

    void Start()
    {
        // 尝试自动查找PlayerMovement实例
        if (playerMovement == null)
        {
            playerMovement = PlayerMovement.Instance; // 尝试通过单例获取
            if (playerMovement == null)
            {
                playerMovement = FindObjectOfType<PlayerMovement>(); // 尝试在场景中查找
                if (playerMovement != null)
                {
                    Debug.LogWarning("HealthUI: PlayerMovement 引用已自动查找并赋值。");
                }
            }
        }

        if (playerMovement == null)
        {
            Debug.LogError("HealthUI: 无法找到 PlayerMovement 引用！请确保玩家对象存在且PlayerMovement脚本已挂载。");
            // 如果玩家引用仍然为空，后续操作将无法进行，但不再直接返回，以便检查Slider
        }

        // 尝试自动查找HealthSlider实例
        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>(); // 尝试在当前GameObject上获取
            if (healthSlider == null)
            {
                // 尝试查找名为 "HealthBarSlider" 的GameObject上的Slider组件
                GameObject sliderObj = GameObject.Find("HealthBarSlider"); 
                if (sliderObj != null)
                {
                    healthSlider = sliderObj.GetComponent<Slider>();
                }
                if (healthSlider != null)
                {
                    Debug.LogWarning("HealthUI: 血条Slider引用已自动查找并赋值（通过名称查找）。");
                }
            }
        }

        if (healthSlider == null)
        {
            Debug.LogError("HealthUI: 无法找到血条Slider引用！请确保Slider对象存在且已正确设置。");
            // 如果Slider引用仍然为空，后续UI更新将无效
        }

        // 只有当两者都找到时才初始化UI
        if (playerMovement != null && healthSlider != null)
        {
            healthSlider.maxValue = playerMovement.maxHP;
            UpdateHealthUI();
        } else {
            Debug.LogWarning("HealthUI: 无法初始化血条UI，缺少必要的引用。请检查上述错误日志。");
        }
    }

    void Update()
    {
        // 实时更新血条UI
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (playerMovement != null && healthSlider != null)
        {
            healthSlider.value = playerMovement.currentHP;
        }
    }
} 