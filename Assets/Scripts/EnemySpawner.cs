using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnData
{
    [Tooltip("敌人预制体")]
    public GameObject enemyPrefab;
    [Tooltip("生成权重 (值越高，生成几率越大)")]
    public int spawnWeight = 1;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("生成设置")]
    [Tooltip("敌人预制体及其生成权重列表")]
    public List<EnemySpawnData> enemySpawnList;
    [Tooltip("场上最大敌人数量阈值")]
    public int maxEnemies = 10;
    [Tooltip("检查生成条件的间隔时间（秒）")]
    public float spawnCheckInterval = 5f;
    [Tooltip("每组生成的敌人数量最小值")]
    public int spawnGroupMin = 2;
    [Tooltip("每组生成的敌人数量最大值")]
    public int spawnGroupMax = 3;

    [Header("生成点设置")]
    [Tooltip("敌人可能的生成点")]
    public Transform[] spawnPoints;

    private float nextSpawnCheckTime;
    private EnemyManager enemyManager;

    void Start()
    {
        // 初始化下一次检查生成条件的时间
        nextSpawnCheckTime = Time.time + spawnCheckInterval;
        // 获取场景中的 EnemyManager 引用
        enemyManager = FindObjectOfType<EnemyManager>();

    }

    void Update()
    {
        // 如果敌人生成列表为空或生成点未设置，或者还没到下一次检查时间，则跳过
        if (enemySpawnList == null || enemySpawnList.Count == 0 || spawnPoints == null || spawnPoints.Length == 0 || Time.time < nextSpawnCheckTime)
        {
            return;
        }

        // 检查当前场上敌人的数量
        int currentEnemies = FindObjectsOfType<Enemy>().Length;

        // 如果当前敌人数量小于阈值，则尝试生成新的敌人
        if (currentEnemies < maxEnemies)
        {
            SpawnEnemyGroup();
            // 更新下一次检查生成条件的时间
            nextSpawnCheckTime = Time.time + spawnCheckInterval;
        }
    }

    void SpawnEnemyGroup()
    {
        // 确定本次生成敌人的数量
        int enemiesToSpawn = Random.Range(spawnGroupMin, spawnGroupMax + 1);

        // 确保有足够的生成点
        if (spawnPoints.Length < enemiesToSpawn)
        {
            Debug.LogWarning("生成点数量不足，无法生成指定数量的敌人！");
            enemiesToSpawn = spawnPoints.Length; // 生成点数量不足时，按实际数量生成
        }

        // 随机选择生成点并生成敌人
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // 随机选择一个可用的生成点
            int randomIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[randomIndex];

            // 根据权重选择一个敌人预制体
            GameObject selectedEnemyPrefab = GetRandomEnemyPrefabByWeight();

            if (selectedEnemyPrefab == null)
            {
                Debug.LogWarning("未找到合适的敌人预制体进行生成，请检查 EnemySpawnList 配置！");
                continue; // 跳过本次生成
            }

            // 在生成点位置实例化敌人
            GameObject newEnemyGO = Instantiate(selectedEnemyPrefab, spawnPoint.position, Quaternion.identity);

            // 获取 Enemy 组件并注册到 EnemyManager
            Enemy newEnemy = newEnemyGO.GetComponent<Enemy>();
            if (newEnemy != null && enemyManager != null)
            {
                enemyManager.RegisterEnemy(newEnemy);
            } else if (newEnemy == null)
            {
                Debug.LogWarning($"生成的敌人预制体 \"{selectedEnemyPrefab.name}\" 没有挂载 Enemy 脚本！");
            }

            // 从可用列表中移除已使用的生成点，确保同一组不重复使用生成点
            availableSpawnPoints.RemoveAt(randomIndex);
        }
    }

    // 根据权重随机获取一个敌人预制体
    GameObject GetRandomEnemyPrefabByWeight()
    {
        int totalWeight = 0;
        foreach (EnemySpawnData data in enemySpawnList)
        {
            totalWeight += data.spawnWeight;
        }

        if (totalWeight == 0) return null; // 避免除以零

        int randomValue = Random.Range(0, totalWeight); // 生成一个0到总权重-1的随机数
        int currentWeight = 0;

        foreach (EnemySpawnData data in enemySpawnList)
        {
            currentWeight += data.spawnWeight;
            if (randomValue < currentWeight)
            {
                return data.enemyPrefab;
            }
        }
        return null; // 不太可能发生，但作为回退
    }
} 