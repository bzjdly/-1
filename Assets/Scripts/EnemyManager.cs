using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("敌人管理")]
    [Tooltip("场景中所有敌人列表（自动收集）")]
    public List<Enemy> enemies = new List<Enemy>();

    [Header("玩家引用")]
    [Tooltip("玩家Transform，所有敌人共用")]
    public Transform player;

    void Awake()
    {
        RefreshEnemyList();
    }

    void Update()
    {
        // 可在此统一调度所有敌人，如全局AI、批量行为等
        // 示例：统计存活敌人数
        // Debug.Log($"当前敌人数：{enemies.Count}");
    }

    public void RefreshEnemyList()
    {
        enemies.Clear();
        enemies.AddRange(FindObjectsOfType<Enemy>());
        // 自动赋值玩家引用
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.player = player;
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
        // 自动赋值玩家引用
        if (enemy != null)
            enemy.player = player;
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);
    }
} 