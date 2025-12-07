using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Water,
    Plain,
    Mountain,
    Castle1,
    Castle2,
    Castle3,
    Castle4
}

public class HexTile
{
    public TileType Type { get; set; }
    public Vector2Int GridPosition { get; set; }
    public Vector3 WorldPosition { get; set; }
}

public class HexagonalMap : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapRadius = 10;
    public float hexSize = 1f;

    [Header("Perlin Noise Settings")]
    public float noiseScale = 0.1f;
    [Range(0f, 1f)] public float waterThreshold = 0.3f;
    [Range(0f, 1f)] public float mountainThreshold = 0.7f;
    public Vector2 noiseOffset;

    [Header("Tile Prefabs")]
    public GameObject[] waterTilePrefabs;
    public GameObject[] plainTilePrefabs;
    public GameObject[] mountainTilePrefabs;
    public GameObject[] castle_1_TilePrefabs;
    public GameObject[] castle_2_TilePrefabs;
    public GameObject[] castle_3_TilePrefabs;
    public GameObject[] castle_4_TilePrefabs;

    private Dictionary<Vector2Int, HexTile> hexGrid = new Dictionary<Vector2Int, HexTile>();
    private Dictionary<Vector2Int, GameObject> tileObjects = new Dictionary<Vector2Int, GameObject>();

    // Словарь для хранения данных о замках: {номер замка, МИРОВАЯ координата Castle1}
    private Dictionary<int, Vector2> castlesDictionary = new Dictionary<int, Vector2>();

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        GenerateHexGrid();
        ApplyPerlinNoise();
        PlaceCastles();
        VisualizeTiles();
        PrintCastlesDictionary(); // Для отладки
    }

    private void GenerateHexGrid()
    {
        hexGrid.Clear();
        castlesDictionary.Clear(); // Очищаем словарь замков при генерации новой карты

        for (int q = -mapRadius; q <= mapRadius; q++)
        {
            for (int r = -mapRadius; r <= mapRadius; r++)
            {
                int s = -q - r;
                if (Mathf.Abs(q) <= mapRadius && Mathf.Abs(r) <= mapRadius && Mathf.Abs(s) <= mapRadius)
                {
                    Vector2Int gridPos = new Vector2Int(q, r);
                    Vector3 worldPos = GridToWorldPosition(gridPos);

                    HexTile tile = new HexTile
                    {
                        GridPosition = gridPos,
                        WorldPosition = worldPos,
                        Type = TileType.Plain
                    };

                    hexGrid.Add(gridPos, tile);
                }
            }
        }
    }

    private Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float q = gridPos.x;
        float r = gridPos.y;

        // Правильное преобразование для flat-top гексов
        float x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
        float y = hexSize * (3f / 2f * r);

        return new Vector3(x, y, 0);
    }

    private void ApplyPerlinNoise()
    {
        foreach (HexTile tile in hexGrid.Values)
        {
            // Используем мировые координаты для согласованного шума
            float noiseX = (tile.WorldPosition.x + noiseOffset.x) * noiseScale;
            float noiseY = (tile.WorldPosition.y + noiseOffset.y) * noiseScale;

            float noiseValue = Mathf.PerlinNoise(noiseX, noiseY);

            if (noiseValue < waterThreshold)
            {
                tile.Type = TileType.Water;
            }
            else if (noiseValue > mountainThreshold)
            {
                tile.Type = TileType.Mountain;
            }
            else
            {
                tile.Type = TileType.Plain;
            }
        }
    }

    private void PlaceCastles()
    {
        Vector2Int[] corners = new Vector2Int[]
        {
            new Vector2Int(mapRadius, 0),             // правый
            new Vector2Int(-mapRadius, 0),            // левый
            new Vector2Int(0, mapRadius),             // верхний правый
            new Vector2Int(-mapRadius, mapRadius),    // верхний левый
            new Vector2Int(0, -mapRadius),            // нижний левый
            new Vector2Int(mapRadius, -mapRadius)     // нижний правый
        };

        int castleNumber = 1;
        for (int i = 0; i < corners.Length; i++)
        {
            bool foundCastleSpot = false;
            for (int ring = 0; ring < mapRadius; ring++)
            {
                var ringTiles = GetTilesInRing(corners[i], ring);

                foreach (var centerTile in ringTiles)
                {
                    if (Check2x2Pattern(centerTile))
                    {
                        // Сохраняем МИРОВУЮ координату Castle1 в словарь
                        Vector3 worldPos = GridToWorldPosition(centerTile);
                        castlesDictionary[castleNumber] = new Vector2(worldPos.x, worldPos.y);

                        ReplaceTilesWithCastle(centerTile, castleNumber);
                        foundCastleSpot = true;
                        castleNumber++;
                        break;
                    }
                }

                if (foundCastleSpot) break;
            }
        }
    }

    // ПРОВЕРКА ПРОСТРАНСТВА ДЛЯ ЗАМКОВ
    private bool Check2x2Pattern(Vector2Int center)
    {
        Vector2Int[] pattern = new Vector2Int[]
        {
            new Vector2Int(center.x, center.y),
            new Vector2Int(center.x + 1, center.y),
            new Vector2Int(center.x, center.y + 1),
            new Vector2Int(center.x + 1, center.y + 1)
        };

        foreach (Vector2Int pos in pattern)
        {
            if (!hexGrid.ContainsKey(pos) || hexGrid[pos].Type != TileType.Plain)
                return false;
        }

        return true;
    }

    // АЛГОРИТМ КОЛЬЦА
    private List<Vector2Int> GetTilesInRing(Vector2Int center, int ring)
    {
        List<Vector2Int> ringTiles = new List<Vector2Int>();

        foreach (Vector2Int pos in hexGrid.Keys)
        {
            int distance = HexDistance(center, pos);
            if (distance == ring)
            {
                ringTiles.Add(pos);
            }
        }

        return ringTiles;
    }

    private int HexDistance(Vector2Int a, Vector2Int b)
    {
        int dq = Mathf.Abs(a.x - b.x);
        int dr = Mathf.Abs(a.y - b.y);
        int ds = Mathf.Abs((-a.x - a.y) - (-b.x - b.y));

        return Mathf.Max(dq, dr, ds);
    }

    // ЗАМЕНА ТАЙЛОВ С УЧЕТОМ НОМЕРА ЗАМКА
    private void ReplaceTilesWithCastle(Vector2Int center, int castleNumber)
    {
        Vector2Int[] pattern = new Vector2Int[]
        {
            new Vector2Int(center.x, center.y),
            new Vector2Int(center.x + 1, center.y),
            new Vector2Int(center.x, center.y + 1),
            new Vector2Int(center.x + 1, center.y + 1)
        };

        // В зависимости от номера замка выбираем соответствующие типы тайлов
        // (можно настроить логику выбора разных типов замков по номеру)
        hexGrid[pattern[0]].Type = TileType.Castle1;
        hexGrid[pattern[1]].Type = TileType.Castle2;
        hexGrid[pattern[2]].Type = TileType.Castle3;
        hexGrid[pattern[3]].Type = TileType.Castle4;
    }

    private void VisualizeTiles()
    {
        // Удаляем старые тайлы
        foreach (var obj in tileObjects.Values)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
        tileObjects.Clear();

        // Создаем новые тайлы
        foreach (HexTile tile in hexGrid.Values)
        {
            GameObject prefab = GetPrefabByType(tile.Type);
            if (prefab != null)
            {
                GameObject tileObj = Instantiate(prefab, transform);
                tileObj.transform.position = tile.WorldPosition;

                tileObj.transform.Rotate(0, 0, 90); // Для flat-top гексов поворот не нужен, они уже должны быть правильно ориентированы в префабе, но поскольку в префабе гекс point-top, то:

                tileObjects.Add(tile.GridPosition, tileObj);
            }
        }
    }

    private GameObject GetPrefabByType(TileType type)
    {
        switch (type)
        {
            case TileType.Water:
                return waterTilePrefabs[UnityEngine.Random.Range(0, waterTilePrefabs.Length)];
            case TileType.Plain:
                return plainTilePrefabs[UnityEngine.Random.Range(0, plainTilePrefabs.Length)];
            case TileType.Mountain:
                return mountainTilePrefabs[UnityEngine.Random.Range(0, mountainTilePrefabs.Length)];
            case TileType.Castle1:
                return castle_1_TilePrefabs[UnityEngine.Random.Range(0, castle_1_TilePrefabs.Length)];
            case TileType.Castle2:
                return castle_2_TilePrefabs[UnityEngine.Random.Range(0, castle_2_TilePrefabs.Length)];
            case TileType.Castle3:
                return castle_3_TilePrefabs[UnityEngine.Random.Range(0, castle_3_TilePrefabs.Length)];
            case TileType.Castle4:
                return castle_4_TilePrefabs[UnityEngine.Random.Range(0, castle_4_TilePrefabs.Length)];
            default:
                return plainTilePrefabs[0];
        }
    }

    // Метод для доступа к словарю замков из других скриптов
    public Dictionary<int, Vector2> GetCastlesDictionary()
    {
        return new Dictionary<int, Vector2>(castlesDictionary);
    }

    // Метод для получения координаты конкретного замка
    public bool TryGetCastlePosition(int castleNumber, out Vector2 castleWorldPosition)
    {
        return castlesDictionary.TryGetValue(castleNumber, out castleWorldPosition);
    }

    // Метод для получения всех замков
    public IEnumerable<KeyValuePair<int, Vector2>> GetAllCastles()
    {
        foreach (var castle in castlesDictionary)
        {
            yield return castle;
        }
    }

    // Метод для отладки - выводит информацию о замках в консоль
    private void PrintCastlesDictionary()
    {
        Debug.Log($"Total castles placed: {castlesDictionary.Count}");
        foreach (var castle in castlesDictionary)
        {
            Debug.Log($"Castle {castle.Key}: World position = {castle.Value}");
        }
    }

    // Визуализация в редакторе для отладки
    void OnDrawGizmos()
    {
        if (hexGrid == null || hexGrid.Count == 0) return;

        Gizmos.color = Color.white;
        foreach (var tile in hexGrid.Values)
        {
            DrawHexagonGizmo(tile.WorldPosition, hexSize);
        }

        // Визуализация позиций замков (МИРОВЫЕ координаты)
        if (castlesDictionary.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (var castlePos in castlesDictionary.Values)
            {
                // Это уже мировые координаты
                Vector3 worldPos = new Vector3(castlePos.x, castlePos.y, 0);
                Gizmos.DrawSphere(worldPos, hexSize * 0.3f);
            }
        }
    }

    // Метод для рисования гексагона
    private void DrawHexagonGizmo(Vector3 center, float size)
    {
        Vector3[] vertices = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i - 30f; // Смещение для flat-top
            angle *= Mathf.Deg2Rad;
            vertices[i] = center + new Vector3(size * Mathf.Cos(angle), size * Mathf.Sin(angle), 0);
        }

        for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(vertices[i], vertices[(i + 1) % 6]);
        }
    }

    void Start()
    {
        GenerateMap();
    }
}