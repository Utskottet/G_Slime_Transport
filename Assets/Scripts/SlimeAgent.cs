using UnityEngine;
using System.Collections.Generic;

public class SlimeAgent : MonoBehaviour
{
    public float inputIntensity = 0f;
    
    private SlimeGameManager manager;
    private SlimeRenderer rend;
    private int id;
    private bool isEnemy;
    
    private List<Vector2Int> activeFrontier = new List<Vector2Int>();
    private List<List<Vector2Int>> history = new List<List<Vector2Int>>();
    
    private int pushBias = 1;

    public void Init(SlimeGameManager mgr, SlimeRenderer r, int _id, Vector2Int _seed, bool _enemy)
    {
        manager = mgr;
        rend = r;
        id = _id;
        isEnemy = _enemy;
        rend.SetId(id);

        List<Vector2Int> initialPixels = new List<Vector2Int>();

        if (isEnemy)
        {
            // Multiple drip fingers from roof
            int numFingers = 25;
            int spacing = manager.gridWidth / numFingers;
            
            for (int i = 0; i < numFingers; i++)
            {
                int centerX = (i * spacing) + (spacing / 2);
                int fingerWidth = Random.Range(3, 8);
                int fingerHeight = Random.Range(5, 15);
                
                for (int y = manager.gridHeight - 1; y > manager.gridHeight - 1 - fingerHeight; y--)
                {
                    for (int dx = -fingerWidth / 2; dx <= fingerWidth / 2; dx++)
                    {
                        int x = centerX + dx;
                        if (x >= 0 && x < manager.gridWidth && manager.grid[x, y] == 0)
                        {
                            if (Random.value > 0.1f)
                            {
                                Claim(x, y, initialPixels);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // Player Start
            Claim(_seed.x, _seed.y, initialPixels);
        }
        
        if (initialPixels.Count > 0) history.Add(initialPixels);
        rend.UpdateTexture();
    }

    public void GameTick()
    {
        if (isEnemy)
        {
            Grow(0.3f);
        }
        else
        {
            if (inputIntensity > 0) Grow(inputIntensity);
            else Shrink();
        }
        
        // Pulse Effect for 3D look
        foreach (var wave in history)
        {
            foreach (var p in wave)
            {
                if (manager.grid[p.x, p.y] == id)
                {
                    int baseThick = manager.gridThickness[p.x, p.y] + 2;
                    if (baseThick > 210) baseThick = 210;
                    manager.gridThickness[p.x, p.y] = (byte)baseThick;
                }
            }
        }
        rend.UpdateTexture();
    }

    private void Grow(float strength)
    {
        if (activeFrontier.Count == 0)
        {
            RebuildFrontier();
            if (activeFrontier.Count == 0) return;
        }

        int growthBudget = Mathf.CeilToInt(20 * strength);
        List<Vector2Int> newWave = new List<Vector2Int>();

        for (int i = 0; i < growthBudget; i++)
        {
            if (activeFrontier.Count == 0) break;

            int rndIndex = Random.Range(0, activeFrontier.Count);
            Vector2Int growSource = activeFrontier[rndIndex];
            
            bool grew = false;
            foreach (var n in GetNeighbors(growSource))
            {
                if (!InBounds(n)) continue;
                byte cell = manager.grid[n.x, n.y];
                
                if (cell == 0 || (cell != 1 && cell != id && TryPush(n.x, n.y, cell)))
                {
                    Claim(n.x, n.y, newWave);
                    grew = true;
                    break;
                }
            }

            if (!grew && IsFullyBlocked(growSource))
            {
                activeFrontier[rndIndex] = activeFrontier[activeFrontier.Count - 1];
                activeFrontier.RemoveAt(activeFrontier.Count - 1);
                i--;
            }
        }

        if (newWave.Count > 0) history.Add(newWave);
    }

    private void Claim(int x, int y, List<Vector2Int> wave)
    {
        manager.grid[x, y] = (byte)id;
        manager.gridThickness[x, y] = 50;
        wave.Add(new Vector2Int(x, y));
        activeFrontier.Add(new Vector2Int(x, y));
    }

    private bool TryPush(int x, int y, int enemyId)
    {
        return Random.value < (0.5f + (pushBias * 0.1f));
    }

    private void Shrink()
    {
        if (history.Count <= 1) return;

        List<Vector2Int> lastWave = history[history.Count - 1];
        foreach (var p in lastWave)
        {
            if (manager.grid[p.x, p.y] == id)
            {
                manager.grid[p.x, p.y] = 0;
                manager.gridThickness[p.x, p.y] = 0;
            }
            activeFrontier.Remove(p);
        }
        history.RemoveAt(history.Count - 1);
    }

    private void RebuildFrontier()
    {
        activeFrontier.Clear();
        for (int x = 0; x < manager.gridWidth; x++)
        {
            for (int y = 0; y < manager.gridHeight; y++)
            {
                if (manager.grid[x, y] == id && !IsFullyBlocked(new Vector2Int(x, y)))
                {
                    activeFrontier.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    private bool IsFullyBlocked(Vector2Int p)
    {
        foreach (var n in GetNeighbors(p))
        {
            if (InBounds(n) && manager.grid[n.x, n.y] != 1 && manager.grid[n.x, n.y] != id)
                return false;
        }
        return true;
    }

    private bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < manager.gridWidth && p.y >= 0 && p.y < manager.gridHeight;
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int p)
    {
        yield return new Vector2Int(p.x, p.y + 1);
        yield return new Vector2Int(p.x, p.y - 1);
        yield return new Vector2Int(p.x + 1, p.y);
        yield return new Vector2Int(p.x - 1, p.y);
    }
}