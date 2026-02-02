using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int levelId;
    public string levelName;
    public int initialHealth;
    public int[] enemyTypes;
    public int enemyCount;
    public float difficulty;
    
    public LevelData(int id, string name, int health, int[] enemies, int count, float diff)
    {
        levelId = id;
        levelName = name;
        initialHealth = health;
        enemyTypes = enemies;
        enemyCount = count;
        difficulty = diff;
    }
}

public static class LevelDataStore
{
    private static Dictionary<int, LevelData> levels = new Dictionary<int, LevelData>();
    
    static LevelDataStore()
    {
        InitializeLevels();
    }
    
    private static void InitializeLevels()
    {
        // Level 1
        levels.Add(1, new LevelData(
            id: 1,
            name: "Level 1",
            health: 100,
            enemies: new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0 },
            count: 3,
            diff: 1.0f
        ));
        
        // // Level 2
        // levels.Add(2, new LevelData(
        //     id: 2,
        //     name: "Level 2",
        //     health: 100,
        //     enemies: new int[] { 0, 1, 1, 2 },
        //     count: 4,
        //     diff: 1.5f
        // ));
        
        // // Level 3
        // levels.Add(3, new LevelData(
        //     id: 3,
        //     name: "Level 3",
        //     health: 100,
        //     enemies: new int[] { 1, 1, 2, 2, 3 },
        //     count: 5,
        //     diff: 2.0f
        // ));
    }
    
    public static LevelData GetLevelById(int id)
    {
        if (levels.ContainsKey(id))
        {
            return levels[id];
        }
        
        Debug.LogWarning($"Level with ID {id} not found. Returning default level.");
        return GetDefaultLevel();
    }
    
    private static LevelData GetDefaultLevel()
    {
        return new LevelData(
            id: 0,
            name: "Default Level",
            health: 100,
            enemies: new int[] { },
            count: 0,
            diff: 1.0f
        );
    }
    
    public static bool LevelExists(int id)
    {
        return levels.ContainsKey(id);
    }
    
    public static int GetLevelCount()
    {
        return levels.Count;
    }
}
