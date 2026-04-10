using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogNode
{
    public string speakerName;
    [TextArea(3, 5)]
    public string text;
    
    // Condition tags
    public string requiresCondition; // Condition that must be true to process this node (for automated routing)
    public string setsCondition;     // Condition set to true when this node is displayed

    // If we have options, we show them. Otherwise, we proceed to nextNode on continue.
    public List<DialogOption> options = new List<DialogOption>();
    public DialogNode nextNode; 
}

[System.Serializable]
public class DialogOption
{
    public string text;
    public string requiresCondition; // Condition needed to make option visible
    public DialogNode targetNode;
}

[System.Serializable]
public class DialogTree
{
    public DialogNode rootNode;
}

/// <summary>
/// Abstract base class for managing character dialog and dialog trees.
/// Inherit from this class and implement the UI-rendering methods.
/// </summary>
public abstract class BaseDialogController : MonoBehaviour
{
    protected DialogTree currentTree;
    protected DialogNode currentNode;

    /// <summary>
    /// Starts the dialog sequence from a given tree.
    /// </summary>
    public virtual void StartDialog(DialogTree tree)
    {
        if (tree == null || tree.rootNode == null) return;
        
        currentTree = tree;
        ShowDialogBox();
        ProcessNode(tree.rootNode);
    }

    /// <summary>
    /// Processes and displays the current node's data.
    /// </summary>
    protected virtual void ProcessNode(DialogNode node)
    {
        if (node == null)
        {
            EndDialog();
            return;
        }

        // Apply any conditions set by this node
        if (!string.IsNullOrEmpty(node.setsCondition))
        {
            ConditionHandler.SetCondition(node.setsCondition, true);
        }

        currentNode = node;
        DisplayDialogText(node.speakerName, node.text);

        if (node.options != null && node.options.Count > 0)
        {
            // Filter options based on conditions
            List<DialogOption> validOptions = new List<DialogOption>();
            foreach (var opt in node.options)
            {
                if (ConditionHandler.HasCondition(opt.requiresCondition))
                {
                    validOptions.Add(opt);
                }
            }

            if (validOptions.Count > 0)
            {
                DisplayOptions(validOptions);
            }
            else
            {
                // Standard continue if all options are hidden
                WaitForContinue();
            }
        }
        else
        {
            // Wait for a standard "continue" action if there are no explicit dialogue choices.
            WaitForContinue();
        }
    }

    /// <summary>
    /// Call this from a UI button when a player selects a specific dialog option.
    /// </summary>
    public virtual void SelectOption(DialogOption option)
    {
        ClearOptions();
        ProcessNode(option.targetNode);
    }

    /// <summary>
    /// Advances the dialog to the next node when there are no choices.
    /// </summary>
    public virtual void ContinueDialog()
    {
        if (currentNode != null && (currentNode.options == null || currentNode.options.Count == 0))
        {
            ProcessNode(currentNode.nextNode);
        }
    }

    /// <summary>
    /// Ends the current dialog sequence.
    /// </summary>
    public virtual void EndDialog()
    {
        currentTree = null;
        currentNode = null;
        HideDialogBox();
    }

    // --- Abstract methods to be implemented by child classes (typically for UI handling) ---

    protected abstract void ShowDialogBox();
    protected abstract void HideDialogBox();
    protected abstract void DisplayDialogText(string speaker, string text);
    protected abstract void DisplayOptions(List<DialogOption> options);
    protected abstract void ClearOptions();
    protected abstract void WaitForContinue();
}
