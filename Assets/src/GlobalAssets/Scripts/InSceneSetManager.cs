using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages multiple in-scene sets, allowing only one to be visible at a time.
/// The first set in the list is the default and will be visible on scene load.
/// </summary>
public class InSceneSetManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("List of prefab instances to manage. The first one will be the default visible set.")]
    private List<GameObject> sets = new List<GameObject>();

    private Dictionary<string, GameObject> setsByName = new Dictionary<string, GameObject>();
    private GameObject currentVisibleSet;

    private void Awake()
    {
        InitializeSets();
    }

    /// <summary>
    /// Initializes the sets, indexes them by name, and shows only the default (first) set.
    /// </summary>
    private void InitializeSets()
    {
        setsByName.Clear();

        // Index all sets by name
        foreach (GameObject set in sets)
        {
            if (set != null)
            {
                setsByName[set.name] = set;
                set.SetActive(false); // Hide all sets initially
            }
        }

        // Show the default set (first in the list)
        if (sets.Count > 0 && sets[0] != null)
        {
            sets[0].SetActive(true);
            currentVisibleSet = sets[0];
        }
    }

    /// <summary>
    /// Switches to a different set by name, hiding the currently visible set.
    /// </summary>
    /// <param name="setName">The name of the set to switch to.</param>
    /// <returns>True if the switch was successful, false otherwise.</returns>
    public bool SwitchToSet(string setName)
    {
        if (!setsByName.ContainsKey(setName))
        {
            Debug.LogWarning($"InSceneSetManager: Set with name '{setName}' not found.");
            return false;
        }

        GameObject targetSet = setsByName[setName];

        // Don't do anything if switching to the already visible set
        if (targetSet == currentVisibleSet)
        {
            return true;
        }

        // Hide the currently visible set
        if (currentVisibleSet != null)
        {
            currentVisibleSet.SetActive(false);
        }

        // Show the target set
        targetSet.SetActive(true);
        currentVisibleSet = targetSet;

        return true;
    }

    /// <summary>
    /// Switches to a set by index in the original list.
    /// </summary>
    /// <param name="index">The index of the set to switch to.</param>
    /// <returns>True if the switch was successful, false otherwise.</returns>
    public bool SwitchToSetByIndex(int index)
    {
        if (index < 0 || index >= sets.Count)
        {
            Debug.LogWarning($"InSceneSetManager: Index {index} is out of range.");
            return false;
        }

        GameObject targetSet = sets[index];
        if (targetSet == null)
        {
            Debug.LogWarning($"InSceneSetManager: Set at index {index} is null.");
            return false;
        }

        return SwitchToSet(targetSet.name);
    }

    /// <summary>
    /// Gets the currently visible set.
    /// </summary>
    /// <returns>The currently visible GameObject set.</returns>
    public GameObject GetCurrentSet()
    {
        return currentVisibleSet;
    }

    /// <summary>
    /// Gets the name of the currently visible set.
    /// </summary>
    /// <returns>The name of the currently visible set, or null if none is visible.</returns>
    public string GetCurrentSetName()
    {
        return currentVisibleSet != null ? currentVisibleSet.name : null;
    }

    /// <summary>
    /// Gets a set by name without switching to it.
    /// </summary>
    /// <param name="setName">The name of the set to retrieve.</param>
    /// <returns>The GameObject if found, null otherwise.</returns>
    public GameObject GetSet(string setName)
    {
        return setsByName.ContainsKey(setName) ? setsByName[setName] : null;
    }
}
