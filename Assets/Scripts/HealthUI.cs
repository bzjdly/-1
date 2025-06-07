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
        if (playerMovement == null)
        {
            Debug.LogError("HealthUI: PlayerMovement 引用未设置！请在Inspector中拖拽赋值。");
            return;
        }

        if (healthSlider == null)
        {
            Debug.LogError("HealthUI: 血条Slider引用未设置！请在Inspector中拖拽赋值。");
            return;
        }

        // 初始化Slider的最大值
        healthSlider.maxValue = playerMovement.maxHP;
        UpdateHealthUI();
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