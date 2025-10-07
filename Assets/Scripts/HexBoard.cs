using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class HexBoard : MonoBehaviour
{
    public enum ColorType { Red, Pink, Green, Yellow, Purple, Orange }

    [Header("Board Settings")]
    public int width = 7;
    public int height = 7;
    public GameObject backgroundTilePrefab;
    public GameObject matchTilePrefab;
    public GameObject clownTilePrefab;
    public float tileScale = 1f;

    [Header("Sprites by Color")]
    public Sprite redSprite;
    public Sprite pinkSprite;
    public Sprite greenSprite;
    public Sprite yellowSprite;
    public Sprite purpleSprite;
    public Sprite orangeSprite;

    private MatchTile[,] matchTiles;
    private bool isGameEnded = false;
    private int totalScore = 0;

    [Header("Mission")]
    public int pierrotMissionCount = 10;
    public int maxMoves = 20;

    [Header("Effects")]
    public GameObject blockCrushEffectPrefab;
    public float crushEffectDuration = 0.5f;

    [Header("Offsets")]
    public float xOffset = 0.9f;
    public float yOffset = 0.78f;

    [Header("Timings")]
    public float tileFallDuration = 0.2f;

    [Header("UI Animation")]
    public GameObject clownImagePrefab;
    public RectTransform clownUITarget;
    public Canvas canvas;
    public float clownFlyDuration = 0.8f;

    private static readonly Vector2Int[] evenColNeighbors = { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1) };
    private static readonly Vector2Int[] oddColNeighbors = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0) };

    void Start()
    {
        GenerateBoard();

        // UI 초기값 설정 이벤트 호출
        GameEvents.PierrotMissionUpdate(pierrotMissionCount);
        GameEvents.MoveCountUpdate(maxMoves);
    }

    public Sprite GetSprite(ColorType type)
    {
        switch (type)
        {
            case ColorType.Red: return redSprite;
            case ColorType.Pink: return pinkSprite;
            case ColorType.Green: return greenSprite;
            case ColorType.Yellow: return yellowSprite;
            case ColorType.Purple: return purpleSprite;
            case ColorType.Orange: return orangeSprite;
            default: return null;
        }
    }

    void GenerateBoard()
    {
        matchTiles = new MatchTile[width, height];

        SpriteRenderer sr = backgroundTilePrefab.GetComponent<SpriteRenderer>();
        float tileWidth = sr.bounds.size.x * tileScale;
        float tileHeight = sr.bounds.size.y * tileScale;
        xOffset = tileWidth * 0.75f;
        yOffset = tileHeight;

        List<Vector2Int> clownPositions = new List<Vector2Int>();
        List<Vector2Int> allPositions = new List<Vector2Int>();
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) allPositions.Add(new Vector2Int(x, y));
        for (int i = 0; i < 3; i++)
        {
            if (allPositions.Count == 0) break;
            int randIdx = Random.Range(0, allPositions.Count);
            clownPositions.Add(allPositions[randIdx]);
            allPositions.RemoveAt(randIdx);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = GetTilePos(x, y);
                GameObject bgTile = Instantiate(backgroundTilePrefab, pos, Quaternion.identity, transform);
                bgTile.GetComponent<SpriteRenderer>().sortingOrder = -1;
                
                bool isClown = clownPositions.Contains(new Vector2Int(x, y));
                GameObject prefabToUse = isClown ? clownTilePrefab : matchTilePrefab;
                GameObject newTileObj = Instantiate(prefabToUse, pos, Quaternion.identity, transform);
                
                MatchTile newTile = newTileObj.GetComponent<MatchTile>();
                ColorType color = (ColorType)Random.Range(0, System.Enum.GetValues(typeof(ColorType)).Length);
                
                newTile.Init(x, y, color, this, isClown ? MatchTile.TileType.Clown : MatchTile.TileType.Normal);
                matchTiles[x, y] = newTile;
            }
        }

        int safeguard = 0;
        while (safeguard++ < 100)
        {
            List<MatchTile> matches = FindAllMatches();
            if (matches.Count == 0) break;

            foreach (var tileToChange in matches)
            {
                if (tileToChange != null && tileToChange.type == MatchTile.TileType.Normal)
                {
                    List<ColorType> possibleColors = System.Enum.GetValues(typeof(ColorType)).Cast<ColorType>().ToList();
                    possibleColors.Remove(tileToChange.color);
                    tileToChange.SetColor(possibleColors[Random.Range(0, possibleColors.Count)]);
                }
            }
        }
    }

    public void AttemptSwap(MatchTile tile, Vector2 dragDirection)
    {
        if (isGameEnded) return;
        if (maxMoves <= 0) return; // 이동 횟수 없으면 스왑 불가

        Vector2Int targetCoords = GetNeighborFromDirection(tile.x, tile.y, dragDirection);

        if (!IsInBounds(targetCoords)) { tile.ResetPosition(); return; }

        MatchTile targetTile = matchTiles[targetCoords.x, targetCoords.y];
        if (targetTile == null || targetTile.type == MatchTile.TileType.Clown)
        { 
            tile.ResetPosition(); 
            return; 
        }

        StartCoroutine(SwapAndCheck(tile, targetTile));
    }

    private IEnumerator SwapAndCheck(MatchTile tileA, MatchTile tileB)
    {
        SwapTiles(tileA, tileB);
        yield return new WaitForSeconds(0.15f);

        List<MatchTile> matches = FindAllMatches();

        if (matches.Count > 0)
        {
            maxMoves--;
            GameEvents.MoveCountUpdate(maxMoves);
            RemoveAndRefill(matches);

            if (maxMoves <= 0 && pierrotMissionCount > 0 && !isGameEnded)
            {
                isGameEnded = true;
                GameEvents.GameOver();
                Debug.Log("게임 오버!");
            }
        }
        else
        {
            yield return new WaitForSeconds(0.15f);
            SwapTiles(tileA, tileB);
        }
    }

    void SwapTiles(MatchTile a, MatchTile b)
    {
        matchTiles[a.x, a.y] = b;
        matchTiles[b.x, b.y] = a;

        int tempX = a.x, tempY = a.y;
        a.x = b.x; a.y = b.y;
        b.x = tempX; b.y = tempY;

        a.transform.position = GetTilePos(a.x, a.y);
        b.transform.position = GetTilePos(b.x, b.y);
    }

    void RemoveAndRefill(List<MatchTile> matches)
    {
        // 점수 계산 및 이벤트 발생
        if (matches.Count > 0)
        {
            int scoreFromMatch = matches.Count * 20;
            totalScore += scoreFromMatch;

            Vector3 averagePos = Vector3.zero;
            foreach (var match in matches)
            {
                averagePos += match.transform.position;
            }
            averagePos /= matches.Count;

            GameEvents.ScoreGained(scoreFromMatch, averagePos);
            GameEvents.ScoreUpdated(totalScore);
        }

        HashSet<MatchTile> triggeredClowns = new HashSet<MatchTile>();
        foreach (var matchedTile in matches)
        {
            if (matchedTile == null) continue;
            var neighbors = (matchedTile.x % 2 == 0) ? evenColNeighbors : oddColNeighbors;
            foreach (var offset in neighbors)
            {
                Vector2Int neighborPos = new Vector2Int(matchedTile.x + offset.x, matchedTile.y + offset.y);
                if (IsInBounds(neighborPos))
                {
                    MatchTile neighborTile = matchTiles[neighborPos.x, neighborPos.y];
                    if (neighborTile != null && neighborTile.type == MatchTile.TileType.Clown)
                    {
                        triggeredClowns.Add(neighborTile);
                    }
                }
            }
        }

        foreach (var clown in triggeredClowns)
        {
            if (pierrotMissionCount > 0)
            {
                // 삐에로 점수 추가
                int clownScore = 300;
                totalScore += clownScore;
                GameEvents.ScoreGained(clownScore, clown.transform.position);
                GameEvents.ScoreUpdated(totalScore);

                // The UI update is now handled by the animation coroutine
                pierrotMissionCount--;

                Debug.Log($"삐에로 등장! 남은 횟수: {pierrotMissionCount}");

                Animator animator = clown.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger("Action");
                }

                StartCoroutine(AnimateClownToUI(clown));
            }
        }

        foreach (var match in matches)
        {
            if (match != null)
            {
                if (blockCrushEffectPrefab != null)
                {
                    Instantiate(blockCrushEffectPrefab, match.transform.position, Quaternion.identity);
                }
                Destroy(match.gameObject);
                matchTiles[match.x, match.y] = null;
            }
        }

        StartCoroutine(DropAndCreateNewTiles());
    }

    private IEnumerator DropAndCreateNewTiles()
    {
        yield return new WaitForSeconds(crushEffectDuration);

        List<Coroutine> fallingTiles = new List<Coroutine>();

        for (int x = 0; x < width; x++)
        {
            List<MatchTile> columnTiles = new List<MatchTile>();
            for (int y = 0; y < height; y++)
            {
                MatchTile tile = matchTiles[x, y];
                if (tile != null && tile.type == MatchTile.TileType.Normal)
                {
                    columnTiles.Add(tile);
                }
            }

            for (int y = 0; y < height; y++)
            {
                if (matchTiles[x, y] != null && matchTiles[x, y].type == MatchTile.TileType.Clown)
                {
                    continue;
                }
                matchTiles[x, y] = null;
            }

            int currentTileIndex = 0;
            for (int y = 0; y < height; y++)
            {
                if (matchTiles[x, y] != null && matchTiles[x, y].type == MatchTile.TileType.Clown)
                {
                    continue;
                }

                if (currentTileIndex < columnTiles.Count)
                {
                    MatchTile tileToPlace = columnTiles[currentTileIndex++];
                    matchTiles[x, y] = tileToPlace;
                    tileToPlace.y = y;
                    
                    Vector3 targetPos = GetTilePos(x, y);
                    fallingTiles.Add(StartCoroutine(tileToPlace.MoveToPosition(targetPos, tileFallDuration)));
                }
                else
                {
                    Vector3 spawnPos = GetTilePos(x, height);
                    GameObject newTileObj = Instantiate(matchTilePrefab, spawnPos, Quaternion.identity, transform);
                    MatchTile newTile = newTileObj.GetComponent<MatchTile>();
                    ColorType color = (ColorType)Random.Range(0, System.Enum.GetValues(typeof(ColorType)).Length);
                    newTile.Init(x, y, color, this, MatchTile.TileType.Normal);
                    matchTiles[x, y] = newTile;

                    Vector3 targetPos = GetTilePos(x, y);
                    fallingTiles.Add(StartCoroutine(newTile.MoveToPosition(targetPos, tileFallDuration)));
                }
            }
        }

        foreach (var coroutine in fallingTiles)
        {
            yield return coroutine;
        }

        yield return new WaitForSeconds(0.1f); 

        List<MatchTile> newMatches = FindAllMatches();
        if (newMatches.Count > 0)
        {
            RemoveAndRefill(newMatches);
        }
    }

    public Vector3 GetTilePos(int x, int y)
    {
        float xPos = x * xOffset;
        float yPos = y * yOffset;
        if (x % 2 != 0)
        {
            yPos -= yOffset / 2f;
        }
        return new Vector3(xPos, yPos, -0.1f);
    }

    #region Match Logic

    List<MatchTile> FindAllMatches()
    {
        HashSet<MatchTile> matchedTiles = new HashSet<MatchTile>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (matchTiles[x, y] != null && matchTiles[x, y].type == MatchTile.TileType.Normal)
                {
                    CheckAxis(matchTiles[x, y], 0, 3, matchedTiles);
                    CheckAxis(matchTiles[x, y], 1, 4, matchedTiles);
                    CheckAxis(matchTiles[x, y], 2, 5, matchedTiles);
                }
            }
        }
        FindRhombusMatches(matchedTiles);
        return matchedTiles.ToList();
    }

    void FindRhombusMatches(HashSet<MatchTile> matchedTiles)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int t1Pos = new Vector2Int(x, y);
                MatchTile t1 = matchTiles[t1Pos.x, t1Pos.y];
                if (t1 == null || t1.type != MatchTile.TileType.Normal) continue;

                int[] dirsToTest = { 0, 1, 2 };
                var t1NeighborOffsets = (t1Pos.x % 2 == 0) ? evenColNeighbors : oddColNeighbors;

                foreach (int dir in dirsToTest)
                {
                    Vector2Int t2Pos = new Vector2Int(t1Pos.x + t1NeighborOffsets[dir].x, t1Pos.y + t1NeighborOffsets[dir].y);

                    if (!IsInBounds(t2Pos)) continue;
                    MatchTile t2 = matchTiles[t2Pos.x, t2Pos.y];
                    if (t2 == null || t2.type != MatchTile.TileType.Normal) continue;

                    List<Vector2Int> t1Neighbors = new List<Vector2Int>();
                    var t1Offsets = (t1Pos.x % 2 == 0) ? evenColNeighbors : oddColNeighbors;
                    foreach (var offset in t1Offsets) t1Neighbors.Add(t1Pos + offset);

                    List<Vector2Int> t2Neighbors = new List<Vector2Int>();
                    var t2Offsets = (t2Pos.x % 2 == 0) ? evenColNeighbors : oddColNeighbors;
                    foreach (var offset in t2Offsets) t2Neighbors.Add(t2Pos + offset);

                    var common = t1Neighbors.Intersect(t2Neighbors).ToList();

                    if (common.Count == 2)
                    {
                        Vector2Int t3Pos = common[0];
                        Vector2Int t4Pos = common[1];

                        if (!IsInBounds(t3Pos) || !IsInBounds(t4Pos)) continue;

                        MatchTile t3 = matchTiles[t3Pos.x, t3Pos.y];
                        MatchTile t4 = matchTiles[t4Pos.x, t4Pos.y];

                        if (t3 != null && t4 != null && t3.type == MatchTile.TileType.Normal && t4.type == MatchTile.TileType.Normal)
                        {
                            if (t1.color == t2.color && t1.color == t3.color && t1.color == t4.color)
                            {
                                matchedTiles.Add(t1);
                                matchedTiles.Add(t2);
                                matchedTiles.Add(t3);
                                matchedTiles.Add(t4);
                            }
                        }
                    }
                }
            }
        }
    }

    void CheckAxis(MatchTile startTile, int dir1, int dir2, HashSet<MatchTile> matchedTiles)
    {
        if (startTile == null || startTile.type != MatchTile.TileType.Normal) return;

        List<MatchTile> line = new List<MatchTile> { startTile };
        ColorType color = startTile.color;

        Vector2Int currentPos = new Vector2Int(startTile.x, startTile.y);
        while (true)
        {
            currentPos = GetNeighborCoords(currentPos.x, currentPos.y, dir1);
            if (!IsInBounds(currentPos)) break;

            MatchTile neighbor = matchTiles[currentPos.x, currentPos.y];
            if (neighbor != null && neighbor.type == MatchTile.TileType.Normal && neighbor.color == color) { line.Add(neighbor); } else { break; }
        }

        currentPos = new Vector2Int(startTile.x, startTile.y);
        while (true)
        {
            currentPos = GetNeighborCoords(currentPos.x, currentPos.y, dir2);
            if (!IsInBounds(currentPos)) break;

            MatchTile neighbor = matchTiles[currentPos.x, currentPos.y];
            if (neighbor != null && neighbor.type == MatchTile.TileType.Normal && neighbor.color == color) { line.Add(neighbor); } else { break; }
        }

        if (line.Count >= 3)
        {
            foreach (var tile in line) matchedTiles.Add(tile);
        }
    }

    Vector2Int GetNeighborCoords(int x, int y, int dirIndex)
    {
        var neighbors = (x % 2 == 0) ? evenColNeighbors : oddColNeighbors;
        Vector2Int offset = neighbors[dirIndex];
        return new Vector2Int(x + offset.x, y + offset.y);
    }

    bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    Vector2Int GetNeighborFromDirection(int x, int y, Vector2 dragDirection)
    {
        var neighbors = (x % 2 == 0) ? evenColNeighbors : oddColNeighbors;
        float maxDot = -1;
        int bestNeighborIndex = -1;

        for (int i = 0; i < neighbors.Length; i++)
        {
            Vector2Int neighborOffset = neighbors[i];
            Vector3 neighborPos = GetTilePos(x + neighborOffset.x, y + neighborOffset.y);
            Vector3 tilePos = GetTilePos(x, y);
            Vector2 directionToNeighbor = (neighborPos - tilePos).normalized;

            float dot = Vector2.Dot(dragDirection.normalized, directionToNeighbor);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestNeighborIndex = i;
            }
        }

        return new Vector2Int(x + neighbors[bestNeighborIndex].x, y + neighbors[bestNeighborIndex].y);
    }

    private IEnumerator AnimateClownToUI(MatchTile clown)
    {
        // Wait for clown's animation if any, assuming 0.5s for the "Action" trigger
        yield return new WaitForSeconds(0.5f);

        if (clownImagePrefab == null || clownUITarget == null || canvas == null)
        {
            Debug.LogWarning("Clown UI animation properties not set in HexBoard.");
            yield break;
        }

        GameObject clownIcon = Instantiate(clownImagePrefab, canvas.transform);
        clownIcon.transform.localScale = Vector3.one; // Ensure scale is correct

        // Convert world point to screen point
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(clown.transform.position);

        // Convert screen point to anchored position in the canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPoint, canvas.worldCamera, out Vector2 anchoredPos);
        clownIcon.GetComponent<RectTransform>().anchoredPosition = anchoredPos;

        Vector3 startWorldPos = clownIcon.transform.position;
        Vector3 endWorldPos = clownUITarget.position;

        Vector3[] path = new Vector3[3];
        path[0] = startWorldPos;
        path[2] = endWorldPos;
        path[1] = (path[0] + path[2]) / 2 + Vector3.down * 150f; // Control point for U-shape

        clownIcon.transform.DOPath(path, clownFlyDuration, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => {
                Destroy(clownIcon);
                GameEvents.PierrotMissionUpdate(pierrotMissionCount);

                if (pierrotMissionCount <= 0 && !isGameEnded)
                {
                    isGameEnded = true;
                    GameEvents.MissionComplete();
                    Debug.Log("미션 클리어!");
                }
            });
    }

    #endregion
}