using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("敌人管理")]
    [Tooltip("场景中所有敌人列表（自动收集）")]
    public List<Enemy> enemies = new List<Enemy>();

    [Header("玩家引用")]
    [Tooltip("玩家Transform，所有敌人共用")]
    public Transform player;

    [Header("寻路设置")]
    [Tooltip("用于寻路的瓦片地图（墙体等障碍物层）")]
    public Tilemap wallTilemap;
    [Tooltip("更新流场的时间间隔（秒）")]
    public float flowFieldUpdateInterval = 0.5f;

    private float nextFlowFieldUpdateTime;

    private Dictionary<Vector3Int, Vector2> flowField = new Dictionary<Vector3Int, Vector2>();
    private Dictionary<Vector3Int, int> distanceField = new Dictionary<Vector3Int, int>();

    private readonly Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(-1, -1, 0)
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else if (Instance != this)
        {
            Destroy(gameObject);
        }

        RefreshEnemyList();
    }

    void Start()
    {
        nextFlowFieldUpdateTime = Time.time + flowFieldUpdateInterval;
        CalculateFlowField();
    }

    void Update()
    {
        if (Time.time >= nextFlowFieldUpdateTime)
        {
            CalculateFlowField();
            nextFlowFieldUpdateTime = Time.time + flowFieldUpdateInterval;
        }
    }

    public void RefreshEnemyList()
    {
        enemies.Clear();
        enemies.AddRange(FindObjectsOfType<Enemy>());
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
        if (enemy != null)
            enemy.player = player;
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);
    }

    private void CalculateFlowField()
    {
        if (player == null || wallTilemap == null)
        {
            Debug.LogWarning("玩家或瓦片地图未设置，无法计算流场！");
            return;
        }

        flowField.Clear();
        distanceField.Clear();

        Vector3Int playerTile = wallTilemap.WorldToCell(player.position);

        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        queue.Enqueue(playerTile);
        distanceField[playerTile] = 0;

        BoundsInt cellBounds = wallTilemap.cellBounds;

        while (queue.Count > 0)
        {
            Vector3Int currentTile = queue.Dequeue();
            int currentDistance = distanceField[currentTile];

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighborTile = currentTile + direction;

                if (!cellBounds.Contains(neighborTile))
                    continue;

                if (wallTilemap.GetTile(neighborTile) != null)
                    continue;

                if (direction.x != 0 && direction.y != 0)
                {
                     Vector3Int corner1 = currentTile + new Vector3Int(direction.x, 0, 0);
                     Vector3Int corner2 = currentTile + new Vector3Int(0, direction.y, 0);

                     if (wallTilemap.GetTile(corner1) != null || wallTilemap.GetTile(corner2) != null)
                         continue;
                }

                if (!distanceField.ContainsKey(neighborTile))
                {
                    distanceField[neighborTile] = currentDistance + 1;
                    queue.Enqueue(neighborTile);
                }
            }
        }

        foreach (var tileDistancePair in distanceField)
        {
            Vector3Int currentTile = tileDistancePair.Key;
            int currentDistance = tileDistancePair.Value;

            if (currentDistance == 0)
            {
                 flowField[currentTile] = Vector2.zero;
                 continue;
            }

            Vector2 bestDirection = Vector2.zero;
            int minDistance = currentDistance;

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighborTile = currentTile + direction;

                 if (!cellBounds.Contains(neighborTile))
                    continue;

                 if (wallTilemap.GetTile(neighborTile) != null)
                    continue;

                if (direction.x != 0 && direction.y != 0)
                {
                     Vector3Int corner1 = currentTile + new Vector3Int(direction.x, 0, 0);
                     Vector3Int corner2 = currentTile + new Vector3Int(0, direction.y, 0);

                     if (wallTilemap.GetTile(corner1) != null || wallTilemap.GetTile(corner2) != null)
                         continue;
                }

                if (distanceField.TryGetValue(neighborTile, out int neighborDistance))
                {
                    if (neighborDistance < minDistance)
                    {
                        minDistance = neighborDistance;
                        Vector2 dirV2 = new Vector2(direction.x, direction.y);
                        bestDirection = dirV2.normalized;
                    } else if (neighborDistance == minDistance) {
                    }
                }
            }
            flowField[currentTile] = bestDirection;
        }
    }

    public Vector2 GetDirectionToPlayer(Vector3 worldPosition)
    {
        if (wallTilemap == null)
        {
            return Vector2.zero;
        }

        Vector3Int tileCoordinate = wallTilemap.WorldToCell(worldPosition);

        if (flowField.TryGetValue(tileCoordinate, out Vector2 direction))
        {
            return direction;
        }

        return Vector2.zero;
    }
} 