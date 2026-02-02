using UnityEngine;

public class CurrentLevelManager : MonoBehaviour
{
    public int health = 100;
    public int[] enemies = {};
    public int currentLevelId;
    public LevelData currentLevel;

    // Constructor that loads a level by ID
    public CurrentLevelManager(int levelId)
    {
        LoadLevel(levelId);
    }

    // Default constructor
    public CurrentLevelManager()
    {
        currentLevelId = 0;
        health = 100;
        enemies = new int[] {};
    }

    public void LoadLevel(int levelId)
    {
        currentLevel = LevelDataStore.GetLevelById(levelId);
        currentLevelId = levelId;
        health = currentLevel.initialHealth;
        enemies = currentLevel.enemyTypes;
    }

    public void ResetLevelLevel()
    {
        GlobalVariables.selected_level = 0;
        health = 100;
        enemies = new int[] {};
        currentLevelId = 0;
        currentLevel = null;
    }
}
