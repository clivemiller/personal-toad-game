using System.Collections.Generic;
using UnityEngine;

// --- DTO (Data Transfer Object) classes for JSON serialization ---
// Unity's JsonUtility cannot properly serialize recursive object references, 
// so we parse from a flat list with string IDs, and then build the real tree.

[System.Serializable]
public class DialogOptionData
{
    public string text;
    public string targetId;
    public string requiresCondition; // Tag to conditionally show this option
}

[System.Serializable]
public class DialogNodeData
{
    public string id;
    public string speakerName;
    [TextArea(3, 5)]
    public string text;
    public string requiresCondition; // Condition needed to jump to this node directly
    public string setsCondition;     // Condition set when the node is read
    public List<DialogOptionData> options;
    public string nextId;
}

[System.Serializable]
public class DialogTreeData
{
    public string rootNodeId;
    public List<string> conditions;  // List of conditions this tree might use (for documentation/initialization)
    public List<DialogNodeData> nodes;
}

public static class DialogParser
{
    /// <summary>
    /// Parses a JSON string and builds a linked DialogTree object.
    /// </summary>
    public static DialogTree Parse(string jsonString)
    {
        // Parse the flat JSON data
        DialogTreeData data = JsonUtility.FromJson<DialogTreeData>(jsonString);
        if (data == null || data.nodes == null || data.nodes.Count == 0)
        {
            Debug.LogError("Failed to parse DialogTreeData or it has no nodes.");
            return null;
        }

        // Dictionary to keep track of nodes by their string IDs
        Dictionary<string, DialogNode> nodeDict = new Dictionary<string, DialogNode>();

        // First pass: instantiate all DialogNodes without linking them
        foreach (var nodeData in data.nodes)
        {
            nodeDict[nodeData.id] = new DialogNode()
            {
                speakerName = nodeData.speakerName,
                text = nodeData.text,
                requiresCondition = nodeData.requiresCondition,
                setsCondition = nodeData.setsCondition,
                options = new List<DialogOption>()
            };
        }

        // Second pass: establish the connections (nextNode and options targeting)
        foreach (var nodeData in data.nodes)
        {
            DialogNode node = nodeDict[nodeData.id];

            // Link Next Node
            if (!string.IsNullOrEmpty(nodeData.nextId) && nodeDict.TryGetValue(nodeData.nextId, out DialogNode nextNode))
            {
                node.nextNode = nextNode;
            }

            // Link Options
            if (nodeData.options != null)
            {
                foreach (var optData in nodeData.options)
                {
                    var newOpt = new DialogOption() 
                    { 
                        text = optData.text,
                        requiresCondition = optData.requiresCondition
                    };
                    
                    if (!string.IsNullOrEmpty(optData.targetId) && nodeDict.TryGetValue(optData.targetId, out DialogNode targetNode))
                    {
                        newOpt.targetNode = targetNode;
                    }
                    
                    node.options.Add(newOpt);
                }
            }
        }

        // Build and return the final tree structure
        DialogTree tree = new DialogTree();
        if (!string.IsNullOrEmpty(data.rootNodeId) && nodeDict.TryGetValue(data.rootNodeId, out DialogNode root))
        {
            tree.rootNode = root;
        }
        else
        {
            Debug.LogError($"Root node ID '{data.rootNodeId}' not found in node list.");
        }

        return tree;
    }
    
    /// <summary>
    /// Helper to parse directly from a Unity TextAsset.
    /// </summary>
    public static DialogTree Parse(TextAsset jsonAsset)
    {
        if (jsonAsset == null) return null;
        return Parse(jsonAsset.text);
    }
}
