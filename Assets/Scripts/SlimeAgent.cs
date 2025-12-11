using UnityEngine;
using System.Collections.Generic;

public class SlimeAgent : MonoBehaviour
{
    public float inputIntensity = 0f;

    private SlimeGameManager manager;
    private SlimeRenderer rend;
    private int id;
    private bool isEnemy;

    private Vector2Int seedCell; // spawn position for respawn

    private List<Vector2Int> activeFrontier = new List<Vector2Int>();
    private List<List<Vector2Int>> history = new List<List<Vector2Int>>();

    private int pushBias = 1;

    // ---------------------------------------------------------
    // INIT
    // ---------------------------------------------------------
    public void Init(SlimeGameManager mgr, SlimeRenderer r, int _id, Vector2Int _seed, bool _enemy)
    {
        manager = mgr;
        rend = r;
        id = _id;
        isEnemy = _enemy;
        seedCell = _seed;

        rend.SetId(id);

        List<Vector2Int> initialPixels = new List<Vector2Int>();

        if (isEnemy)
        {
            // Random enemy drop along top row
            for (int x = 0; x < manager.gridWidth; x++)
            {
                if (Random.value > 0.05f) continue;
                int y = manager.gridHeight - 1;
                if (manager.grid[x, y] == 0)
                    Claim(x, y, initialPixels);
            }
        }
        else
        {
            // Player start from seed
            Claim(seedCell.x, seedCell.y, initialPixels);
        }

        if (initialPixels.Count > 0)
            history.Add(initialPixels);

        rend.UpdateTexture();
    }

    // ---------------------------------------------------------
    // MAIN TICK
    // ---------------------------------------------------------
    public void GameTick()
    {
        if (isEnemy)
        {
            Grow(0.3f);
        }
        else
        {
            if (inputIntensity > 0f)
            {
                // NEW: if player slime is completely gone, respawn at seed
                RespawnIfDead();
                Grow(inputIntensity);
            }
            else
            {
                Shrink();
            }
        }

        // Pulse thickness for 3D look
        int pulse = (int)(Mathf.PingPong(Time.time * 50f, 40f));
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

    // ---------------------------------------------------------
    // GROW / RESPAWN
    // ---------------------------------------------------------
    void RespawnIfDead()
    {
        if (isEnemy) return;          // only players respawn this way
        if (HasAnyCells()) return;    // still alive somewhere

        history.Clear();
        activeFrontier.Clear();

        List<Vector2Int> wave = new List<Vector2Int>();

        // Clamp seed into bounds
        int sx = Mathf.Clamp(seedCell.x, 0, manager.gridWidth - 1);
        int sy = Mathf.Clamp(seedCell.y, 0, manager.gridHeight - 1);

        // If seed is on a wall, try to nudge to a nearby free cell
        if (manager.grid[sx, sy] == 1)
        {
            bool found = false;
            int maxRadius = 5;
            for (int r = 1; r <= maxRadius && !found; r++)
            {
                for (int dx = -r; dx <= r && !found; dx++)
                {
                    for (int dy = -r; dy <= r && !found; dy++)
                    {
                        int nx = sx + dx;
                        int ny = sy + dy;
                        if (nx < 0 || nx >= manager.gridWidth || ny < 0 || ny >= manager.gridHeight)
                            continue;
                        if (manager.grid[nx, ny] == 0)
                        {
                            sx = nx;
                            sy = ny;
                            found = true;
                        }
                    }
                }
            }
            if (!found) return; // nowhere safe to respawn
        }

        Claim(sx, sy, wave);
        if (wave.Count > 0)
            history.Add(wave);
    }

    void Grow(float strength)
    {
        // If we have no frontier but we exist, rebuild frontier.
        if (activeFrontier.Count == 0)
        {
            RebuildFrontier();
            if (activeFrontier.Count == 0)
                return; // truly dead or stuck (enemy) – players will be handled by RespawnIfDead
        }

        int growthBudget = Mathf.CeilToInt(20 * strength);
        List<Vector2Int> newWave = new List<Vector2Int>();

        for (int i = 0; i < growthBudget; i++)
        {
            if (activeFrontier.Count == 0)
                break;

            int rndIndex = Random.Range(0, activeFrontier.Count);
            Vector2Int growSource = activeFrontier[rndIndex];
            bool grew = false;

            foreach (var n in GetNeighbors(growSource))
            {
                if (!InBounds(n)) continue;

                byte cell = manager.grid[n.x, n.y];

                // Grow if empty OR if we can push other slime
                if (cell == 0 || (cell != 1 && cell != id && TryPush(n.x, n.y, cell)))
                {
                    Claim(n.x, n.y, newWave);
                    grew = true;
                    break;
                }
            }

            // Remove from frontier if fully blocked
            if (!grew && IsFullyBlocked(growSource))
            {
                activeFrontier[rndIndex] = activeFrontier[activeFrontier.Count - 1];
                activeFrontier.RemoveAt(activeFrontier.Count - 1);
                i--; // don't lose a budget step
            }
        }

        if (newWave.Count > 0)
            history.Add(newWave);
    }

    void Claim(int x, int y, List<Vector2Int> wave)
    {
        manager.grid[x, y] = (byte)id;
        manager.gridThickness[x, y] = 50;
        Vector2Int p = new Vector2Int(x, y);
        wave.Add(p);
        activeFrontier.Add(p);
    }

    bool TryPush(int x, int y, int enemyId)
    {
        return Random.value < (0.5f + (pushBias * 0.1f));
    }

    // ---------------------------------------------------------
    // SHRINK
    // ---------------------------------------------------------
    void Shrink()
    {
        // Never delete the first wave (seed). Otherwise slime can die forever.
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

    // ---------------------------------------------------------
    // FRONTIER / UTILS
    // ---------------------------------------------------------
    void RebuildFrontier()
    {
        activeFrontier.Clear();

        for (int x = 0; x < manager.gridWidth; x++)
        {
            for (int y = 0; y < manager.gridHeight; y++)
            {
                if (manager.grid[x, y] != id)
                    continue;

                Vector2Int p = new Vector2Int(x, y);
                foreach (var n in GetNeighbors(p))
                {
                    if (!InBounds(n)) continue;
                    if (manager.grid[n.x, n.y] != id)
                    {
                        activeFrontier.Add(p);
                        break;
                    }
                }
            }
        }
    }

    bool IsFullyBlocked(Vector2Int p)
    {
        foreach (var n in GetNeighbors(p))
        {
            if (!InBounds(n)) continue;

            byte cell = manager.grid[n.x, n.y];
            if (cell == 0 || (cell != 1 && cell != id))
                return false; // there is somewhere we could grow/push
        }
        return true;
    }

    bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < manager.gridWidth &&
               p.y >= 0 && p.y < manager.gridHeight;
    }

    bool HasAnyCells()
    {
        for (int x = 0; x < manager.gridWidth; x++)
        {
            for (int y = 0; y < manager.gridHeight; y++)
            {
                if (manager.grid[x, y] == id)
                    return true;
            }
        }
        return false;
    }

    IEnumerable<Vector2Int> GetNeighbors(Vector2Int p)
    {
        yield return new Vector2Int(p.x, p.y + 1);
        yield return new Vector2Int(p.x, p.y - 1);
        yield return new Vector2Int(p.x + 1, p.y);
        yield return new Vector2Int(p.x - 1, p.y);
    }
}
