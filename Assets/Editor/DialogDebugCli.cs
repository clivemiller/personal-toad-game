// This script was made for debugging my code. It was written with the assistance of AI

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DialogDebugCli
{
    // path to json dialog format
    private const string DefaultDialogPath = "Assets/src/Scripts/Dialog/ExampleDialog.json";

    public static void Run()
    {
        DialogCliArgs args = DialogCliArgs.Parse(Environment.GetCommandLineArgs());
        if (args.ShowHelp)
        {
            PrintUsage();
            Exit(0);
            return;
        }

        if (Application.isBatchMode && !args.ForceUnityInteractive)
        {
            Console.WriteLine("Unity batchmode does not provide a reliable interactive terminal on Windows.");
            Console.WriteLine("Use the PowerShell debugger instead:");
            Console.WriteLine("  powershell -ExecutionPolicy Bypass -File tools/DialogDebug.ps1 -Dialog Assets/src/Scripts/Dialog/ExampleDialog.json");
            Console.WriteLine("If you still want to try Unity stdin, pass --force-unity-interactive.");
            Exit(1);
            return;
        }

        string dialogPath = ResolveProjectPath(string.IsNullOrEmpty(args.DialogPath) ? DefaultDialogPath : args.DialogPath);
        if (!File.Exists(dialogPath))
        {
            Console.WriteLine("Dialog file not found: " + dialogPath);
            Exit(1);
            return;
        }

        if (args.ResetConditions)
        {
            ConditionHandler.ResetAllConditions();
        }

        foreach (string condition in args.InitialConditions)
        {
            ConditionHandler.SetCondition(condition, true);
        }

        string json = File.ReadAllText(dialogPath);
        DialogTree tree = DialogParser.Parse(json);
        if (tree == null || tree.rootNode == null)
        {
            Console.WriteLine("Failed to parse dialog tree: " + dialogPath);
            Exit(1);
            return;
        }

        DialogTreeData rawData = JsonUtility.FromJson<DialogTreeData>(json);
        GameObject host = new GameObject("DialogDebugCliController");
        DialogDebugCliController controller = host.AddComponent<DialogDebugCliController>();
        controller.Initialize(tree, rawData);

        Console.WriteLine("Loaded dialog: " + dialogPath);
        Console.WriteLine("Type 'help' for commands. Press Enter to continue nodes without choices.");
        Console.WriteLine(string.Empty);

        controller.StartDialog(tree);
        controller.RunPrompt();

        UnityEngine.Object.DestroyImmediate(host);
        Exit(0);
    }

    private static string ResolveProjectPath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(projectRoot, path));
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Dialog CLI");
        Console.WriteLine(string.Empty);
        Console.WriteLine("Usage:");
        Console.WriteLine("  powershell -ExecutionPolicy Bypass -File tools/DialogDebug.ps1 -Dialog Assets/src/Scripts/Dialog/ExampleDialog.json");
        Console.WriteLine(string.Empty);
        Console.WriteLine("Unity editor hook:");
        Console.WriteLine("  Unity.exe -projectPath <project> -batchmode -executeMethod DialogDebugCli.Run -- --help");
        Console.WriteLine(string.Empty);
        Console.WriteLine("Options:");
        Console.WriteLine("  --dialog <path>          Dialog JSON path. Defaults to " + DefaultDialogPath);
        Console.WriteLine("  --condition <name>       Pre-set a condition before starting. Can be repeated.");
        Console.WriteLine("  --reset-conditions       Clear tracked dialog conditions before starting.");
        Console.WriteLine("  --force-unity-interactive Try the Unity-hosted prompt anyway.");
        Console.WriteLine("  --help                   Print this help.");
        Console.WriteLine(string.Empty);
        Console.WriteLine("Interactive commands:");
        Console.WriteLine("  <number>                 Select a visible option.");
        Console.WriteLine("  <enter>, c, continue     Continue when the node has no visible options.");
        Console.WriteLine("  conditions               Print known condition states.");
        Console.WriteLine("  set <condition>          Set a condition to true.");
        Console.WriteLine("  unset <condition>        Set a condition to false.");
        Console.WriteLine("  nodes                    Print parsed node ids.");
        Console.WriteLine("  goto <nodeId>            Jump to a parsed node id.");
        Console.WriteLine("  restart                  Restart from the root node.");
        Console.WriteLine("  quit                     End the debugger.");
    }

    private static void Exit(int code)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(code);
        }
    }

    private sealed class DialogCliArgs
    {
        public string DialogPath;
        public bool ResetConditions;
        public bool ShowHelp;
        public bool ForceUnityInteractive;
        public readonly List<string> InitialConditions = new List<string>();

        public static DialogCliArgs Parse(string[] commandLineArgs)
        {
            DialogCliArgs parsed = new DialogCliArgs();

            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                string arg = commandLineArgs[i];

                if (arg == "--help" || arg == "-h")
                {
                    parsed.ShowHelp = true;
                    continue;
                }

                if (arg == "--reset-conditions")
                {
                    parsed.ResetConditions = true;
                    continue;
                }

                if (arg == "--force-unity-interactive")
                {
                    parsed.ForceUnityInteractive = true;
                    continue;
                }

                if (arg == "--dialog" && i + 1 < commandLineArgs.Length)
                {
                    parsed.DialogPath = commandLineArgs[++i];
                    continue;
                }

                if (arg.StartsWith("--dialog=", StringComparison.Ordinal))
                {
                    parsed.DialogPath = arg.Substring("--dialog=".Length);
                    continue;
                }

                if (arg == "--condition" && i + 1 < commandLineArgs.Length)
                {
                    parsed.InitialConditions.Add(commandLineArgs[++i]);
                    continue;
                }

                if (arg.StartsWith("--condition=", StringComparison.Ordinal))
                {
                    parsed.InitialConditions.Add(arg.Substring("--condition=".Length));
                }
            }

            return parsed;
        }
    }
}

internal sealed class DialogDebugCliController : BaseDialogController
{
    private readonly Dictionary<string, DialogNode> nodesById = new Dictionary<string, DialogNode>();
    private readonly List<string> knownConditions = new List<string>();
    private readonly List<DialogOption> visibleOptions = new List<DialogOption>();

    private DialogTree rootTree;
    private bool waitingForContinue;
    private bool isRunning;

    public void Initialize(DialogTree tree, DialogTreeData rawData)
    {
        rootTree = tree;
        nodesById.Clear();
        knownConditions.Clear();
        AddKnownConditions(rawData);
        IndexNodes(tree != null ? tree.rootNode : null, new HashSet<DialogNode>());
    }

    public void RunPrompt()
    {
        while (isRunning)
        {
            Console.Write("> ");
            string input = Console.ReadLine();
            if (input == null)
            {
                EndDialog();
                break;
            }

            HandleInput(input.Trim());
        }
    }

    public override void StartDialog(DialogTree tree)
    {
        isRunning = true;
        base.StartDialog(tree);
    }

    public override void EndDialog()
    {
        visibleOptions.Clear();
        waitingForContinue = false;
        isRunning = false;
        base.EndDialog();
    }

    protected override void ShowDialogBox()
    {
    }

    protected override void HideDialogBox()
    {
        Console.WriteLine("Dialog ended.");
    }

    protected override void DisplayDialogText(string speaker, string text)
    {
        Console.WriteLine(string.Empty);
        Console.WriteLine("[" + GetNodeLabel(currentNode) + "]");
        if (!string.IsNullOrEmpty(speaker))
        {
            Console.WriteLine(speaker + ":");
        }

        Console.WriteLine(string.IsNullOrEmpty(text) ? "(no text)" : text);

        if (!string.IsNullOrEmpty(currentNode != null ? currentNode.setsCondition : null))
        {
            Console.WriteLine("Set condition: " + currentNode.setsCondition);
        }
    }

    protected override void DisplayOptions(List<DialogOption> options)
    {
        visibleOptions.Clear();
        waitingForContinue = false;

        if (options != null)
        {
            visibleOptions.AddRange(options);
        }

        if (visibleOptions.Count == 0)
        {
            Console.WriteLine("No visible options. Press Enter to continue.");
            waitingForContinue = true;
            return;
        }

        Console.WriteLine(string.Empty);
        Console.WriteLine("Options:");
        for (int i = 0; i < visibleOptions.Count; i++)
        {
            DialogOption option = visibleOptions[i];
            string target = option != null && option.targetNode != null ? option.targetNode.id : "(end)";
            Console.WriteLine("  " + (i + 1) + ". " + option.text + " -> " + target);
        }
    }

    protected override void ClearOptions()
    {
        visibleOptions.Clear();
    }

    protected override void WaitForContinue()
    {
        visibleOptions.Clear();
        waitingForContinue = true;
        Console.WriteLine(string.Empty);
        Console.WriteLine("Press Enter to continue.");
    }

    private void HandleInput(string input)
    {
        if (string.IsNullOrEmpty(input) || input == "c" || input == "continue")
        {
            if (waitingForContinue)
            {
                waitingForContinue = false;
                ContinueDialog();
            }
            else
            {
                Console.WriteLine("Choose an option number, or type 'help'.");
            }

            return;
        }

        if (input == "help")
        {
            PrintInteractiveHelp();
            return;
        }

        if (input == "quit" || input == "exit" || input == "q")
        {
            EndDialog();
            return;
        }

        if (input == "restart")
        {
            StartDialog(rootTree);
            return;
        }

        if (input == "conditions")
        {
            PrintConditions();
            return;
        }

        if (input == "nodes")
        {
            PrintNodes();
            return;
        }

        if (input.StartsWith("set ", StringComparison.Ordinal))
        {
            SetCondition(input.Substring(4).Trim(), true);
            return;
        }

        if (input.StartsWith("unset ", StringComparison.Ordinal))
        {
            SetCondition(input.Substring(6).Trim(), false);
            return;
        }

        if (input.StartsWith("goto ", StringComparison.Ordinal))
        {
            GoToNode(input.Substring(5).Trim());
            return;
        }

        int optionNumber;
        if (int.TryParse(input, out optionNumber))
        {
            SelectVisibleOption(optionNumber);
            return;
        }

        Console.WriteLine("Unknown command: " + input);
    }

    private void SelectVisibleOption(int optionNumber)
    {
        if (optionNumber < 1 || optionNumber > visibleOptions.Count)
        {
            Console.WriteLine("Option number out of range.");
            return;
        }

        waitingForContinue = false;
        SelectOption(visibleOptions[optionNumber - 1]);
    }

    private void SetCondition(string conditionName, bool value)
    {
        if (string.IsNullOrEmpty(conditionName))
        {
            Console.WriteLine("Condition name is required.");
            return;
        }

        ConditionHandler.SetCondition(conditionName, value);
        AddKnownCondition(conditionName);
        Console.WriteLine(conditionName + " = " + (value ? "true" : "false"));
        RedrawCurrentNode();
    }

    private void GoToNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            Console.WriteLine("Node id is required.");
            return;
        }

        DialogNode node;
        if (!nodesById.TryGetValue(nodeId, out node))
        {
            Console.WriteLine("Unknown node id: " + nodeId);
            return;
        }

        waitingForContinue = false;
        ProcessNode(node);
    }

    private void RedrawCurrentNode()
    {
        if (currentNode != null)
        {
            ProcessNode(currentNode);
        }
    }

    private void PrintInteractiveHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  <number>              Select a visible option.");
        Console.WriteLine("  <enter>, c, continue  Continue when no choices are visible.");
        Console.WriteLine("  conditions            Print known condition states.");
        Console.WriteLine("  set <condition>       Set a condition to true and redraw current node.");
        Console.WriteLine("  unset <condition>     Set a condition to false and redraw current node.");
        Console.WriteLine("  nodes                 Print parsed node ids.");
        Console.WriteLine("  goto <nodeId>         Jump to a parsed node id.");
        Console.WriteLine("  restart               Restart from the root node.");
        Console.WriteLine("  quit                  End the debugger.");
    }

    private void PrintConditions()
    {
        if (knownConditions.Count == 0)
        {
            Console.WriteLine("No known conditions in this dialog.");
            return;
        }

        Console.WriteLine("Conditions:");
        for (int i = 0; i < knownConditions.Count; i++)
        {
            string condition = knownConditions[i];
            Console.WriteLine("  " + condition + " = " + (ConditionHandler.HasCondition(condition) ? "true" : "false"));
        }
    }

    private void PrintNodes()
    {
        if (nodesById.Count == 0)
        {
            Console.WriteLine("No parsed node ids.");
            return;
        }

        Console.WriteLine("Nodes:");
        List<string> nodeIds = new List<string>(nodesById.Keys);
        nodeIds.Sort(StringComparer.Ordinal);
        foreach (string nodeId in nodeIds)
        {
            Console.WriteLine("  " + nodeId);
        }
    }

    private void AddKnownConditions(DialogTreeData rawData)
    {
        if (rawData == null)
        {
            return;
        }

        if (rawData.conditions != null)
        {
            for (int i = 0; i < rawData.conditions.Count; i++)
            {
                AddKnownCondition(rawData.conditions[i]);
            }
        }

        if (rawData.nodes == null)
        {
            return;
        }

        for (int i = 0; i < rawData.nodes.Count; i++)
        {
            DialogNodeData node = rawData.nodes[i];
            AddKnownCondition(node.requiresCondition);
            AddKnownCondition(node.setsCondition);

            if (node.options == null)
            {
                continue;
            }

            for (int optionIndex = 0; optionIndex < node.options.Count; optionIndex++)
            {
                AddKnownCondition(node.options[optionIndex].requiresCondition);
            }
        }
    }

    private void AddKnownCondition(string conditionName)
    {
        if (string.IsNullOrEmpty(conditionName) || knownConditions.Contains(conditionName))
        {
            return;
        }

        knownConditions.Add(conditionName);
    }

    private void IndexNodes(DialogNode node, HashSet<DialogNode> visited)
    {
        if (node == null || visited.Contains(node))
        {
            return;
        }

        visited.Add(node);

        if (!string.IsNullOrEmpty(node.id) && !nodesById.ContainsKey(node.id))
        {
            nodesById.Add(node.id, node);
        }

        IndexNodes(node.nextNode, visited);

        if (node.options == null)
        {
            return;
        }

        for (int i = 0; i < node.options.Count; i++)
        {
            IndexNodes(node.options[i].targetNode, visited);
        }
    }

    private string GetNodeLabel(DialogNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.id))
        {
            return "node";
        }

        return node.id;
    }
}
