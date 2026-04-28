# This script was made for debugging my code. It was written with the assistance of AI

[CmdletBinding()]
param(
    [string]$Dialog = "Assets/src/Scripts/Dialog/ExampleDialog.json",
    [string[]]$Condition = @(),
    [switch]$ResetConditions,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$conditions = @{}
$knownConditions = New-Object System.Collections.Generic.List[string]
$nodesById = @{}
$visibleOptions = @()
$currentNode = $null
$isRunning = $true

function Show-Usage {
    Write-Host "Dialog debugger"
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  .\tools\DialogDebug.ps1 [-Dialog Assets/src/Scripts/Dialog/ExampleDialog.json] [-Condition LearnedName]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Dialog <path>       Dialog JSON path. Defaults to Assets/src/Scripts/Dialog/ExampleDialog.json"
    Write-Host "  -Condition <name>    Pre-set one or more conditions before starting."
    Write-Host "  -ResetConditions     Accepted for parity with the Unity hook; conditions are in-memory per run."
    Write-Host "  -Help                Print this help."
    Write-Host ""
    Write-Host "Interactive commands:"
    Write-Host "  <number>             Select a visible option."
    Write-Host "  <enter>, c, continue Continue when the node has no visible options."
    Write-Host "  conditions           Print known condition states."
    Write-Host "  set <condition>      Set a condition to true."
    Write-Host "  unset <condition>    Set a condition to false."
    Write-Host "  nodes                Print parsed node ids."
    Write-Host "  goto <nodeId>        Jump to a parsed node id."
    Write-Host "  restart              Restart from the root node."
    Write-Host "  quit                 End the debugger."
}

function Resolve-DialogPath([string]$path) {
    if ([System.IO.Path]::IsPathRooted($path)) {
        return [System.IO.Path]::GetFullPath($path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $projectRoot $path))
}

function Add-KnownCondition([string]$conditionName) {
    if ([string]::IsNullOrEmpty($conditionName)) {
        return
    }

    if (-not $knownConditions.Contains($conditionName)) {
        [void]$knownConditions.Add($conditionName)
    }
}

function Has-Condition([string]$conditionName) {
    if ([string]::IsNullOrEmpty($conditionName)) {
        return $true
    }

    return $conditions.ContainsKey($conditionName) -and $conditions[$conditionName]
}

function Set-DialogCondition([string]$conditionName, [bool]$value) {
    if ([string]::IsNullOrWhiteSpace($conditionName)) {
        Write-Host "Condition name is required."
        return
    }

    $conditions[$conditionName] = $value
    Add-KnownCondition $conditionName
    Write-Host "$conditionName = $($value.ToString().ToLowerInvariant())"

    if ($null -ne $currentNode) {
        Show-Node $currentNode
    }
}

function Build-DialogTree($dialogData) {
    if ($null -eq $dialogData -or $null -eq $dialogData.nodes -or $dialogData.nodes.Count -eq 0) {
        throw "Dialog data has no nodes."
    }

    $nodesById.Clear()
    $knownConditions.Clear()

    foreach ($conditionName in @($dialogData.conditions)) {
        Add-KnownCondition $conditionName
    }

    foreach ($nodeData in $dialogData.nodes) {
        if ([string]::IsNullOrEmpty($nodeData.id)) {
            throw "Every dialog node needs an id."
        }

        $nodesById[$nodeData.id] = [pscustomobject]@{
            Id = $nodeData.id
            SpeakerName = $nodeData.speakerName
            Text = $nodeData.text
            RequiresCondition = $nodeData.requiresCondition
            SetsCondition = $nodeData.setsCondition
            Options = @()
            NextNode = $null
        }

        Add-KnownCondition $nodeData.requiresCondition
        Add-KnownCondition $nodeData.setsCondition

        foreach ($optionData in @($nodeData.options)) {
            Add-KnownCondition $optionData.requiresCondition
        }
    }

    foreach ($nodeData in $dialogData.nodes) {
        $node = $nodesById[$nodeData.id]

        if (-not [string]::IsNullOrEmpty($nodeData.nextId)) {
            if (-not $nodesById.ContainsKey($nodeData.nextId)) {
                Write-Host "Warning: node '$($nodeData.id)' references missing nextId '$($nodeData.nextId)'."
            } else {
                $node.NextNode = $nodesById[$nodeData.nextId]
            }
        }

        $options = @()
        foreach ($optionData in @($nodeData.options)) {
            $targetNode = $null
            if (-not [string]::IsNullOrEmpty($optionData.targetId)) {
                if ($nodesById.ContainsKey($optionData.targetId)) {
                    $targetNode = $nodesById[$optionData.targetId]
                } else {
                    Write-Host "Warning: option '$($optionData.text)' references missing targetId '$($optionData.targetId)'."
                }
            }

            $options += [pscustomobject]@{
                Text = $optionData.text
                RequiresCondition = $optionData.requiresCondition
                TargetNode = $targetNode
                TargetId = $optionData.targetId
            }
        }

        $node.Options = @($options)
    }

    if ([string]::IsNullOrEmpty($dialogData.rootNodeId) -or -not $nodesById.ContainsKey($dialogData.rootNodeId)) {
        throw "Root node ID '$($dialogData.rootNodeId)' not found in node list."
    }

    return $nodesById[$dialogData.rootNodeId]
}

function Show-Node($node) {
    if ($null -eq $node) {
        End-Dialog
        return
    }

    $script:currentNode = $node
    $script:visibleOptions = @()

    if (-not [string]::IsNullOrEmpty($node.SetsCondition)) {
        $conditions[$node.SetsCondition] = $true
        Add-KnownCondition $node.SetsCondition
    }

    Write-Host ""
    Write-Host "[$($node.Id)]"
    if (-not [string]::IsNullOrEmpty($node.SpeakerName)) {
        Write-Host "$($node.SpeakerName):"
    }

    if ([string]::IsNullOrEmpty($node.Text)) {
        Write-Host "(no text)"
    } else {
        Write-Host $node.Text
    }

    if (-not [string]::IsNullOrEmpty($node.SetsCondition)) {
        Write-Host "Set condition: $($node.SetsCondition)"
    }

    $validOptions = @()
    foreach ($option in @($node.Options)) {
        if (Has-Condition $option.RequiresCondition) {
            $validOptions += $option
        }
    }

    if ($validOptions.Count -gt 0) {
        $script:visibleOptions = @($validOptions)
        Write-Host ""
        Write-Host "Options:"
        for ($i = 0; $i -lt $script:visibleOptions.Count; $i++) {
            $option = $script:visibleOptions[$i]
            $target = if ($null -ne $option.TargetNode) { $option.TargetNode.Id } else { "(end)" }
            Write-Host ("  {0}. {1} -> {2}" -f ($i + 1), $option.Text, $target)
        }
    } else {
        Write-Host ""
        Write-Host "Press Enter to continue."
    }
}

function Continue-Dialog {
    if ($script:visibleOptions.Count -gt 0) {
        Write-Host "Choose an option number, or type 'help'."
        return
    }

    Show-Node $script:currentNode.NextNode
}

function End-Dialog {
    $script:isRunning = $false
    $script:currentNode = $null
    $script:visibleOptions = @()
    Write-Host "Dialog ended."
}

function Show-Conditions {
    if ($knownConditions.Count -eq 0) {
        Write-Host "No known conditions in this dialog."
        return
    }

    Write-Host "Conditions:"
    foreach ($conditionName in $knownConditions) {
        $value = (Has-Condition $conditionName).ToString().ToLowerInvariant()
        Write-Host "  $conditionName = $value"
    }
}

function Show-Nodes {
    if ($nodesById.Count -eq 0) {
        Write-Host "No parsed node ids."
        return
    }

    Write-Host "Nodes:"
    foreach ($nodeId in ($nodesById.Keys | Sort-Object)) {
        Write-Host "  $nodeId"
    }
}

function Show-InteractiveHelp {
    Write-Host "Commands:"
    Write-Host "  <number>              Select a visible option."
    Write-Host "  <enter>, c, continue  Continue when no choices are visible."
    Write-Host "  conditions            Print known condition states."
    Write-Host "  set <condition>       Set a condition to true and redraw current node."
    Write-Host "  unset <condition>     Set a condition to false and redraw current node."
    Write-Host "  nodes                 Print parsed node ids."
    Write-Host "  goto <nodeId>         Jump to a parsed node id."
    Write-Host "  restart               Restart from the root node."
    Write-Host "  quit                  End the debugger."
}

if ($Help) {
    Show-Usage
    exit 0
}

$dialogPath = Resolve-DialogPath $Dialog
if (-not (Test-Path -LiteralPath $dialogPath -PathType Leaf)) {
    throw "Dialog file not found: $dialogPath"
}

foreach ($conditionName in $Condition) {
    if (-not [string]::IsNullOrWhiteSpace($conditionName)) {
        $conditions[$conditionName] = $true
        Add-KnownCondition $conditionName
    }
}

$dialogData = Get-Content -Raw -LiteralPath $dialogPath | ConvertFrom-Json
$rootNode = Build-DialogTree $dialogData

Write-Host "Loaded dialog: $dialogPath"
Write-Host "Type 'help' for commands. Press Enter to continue nodes without choices."
Show-Node $rootNode

while ($isRunning) {
    Write-Host -NoNewline "> "
    $inputText = [Console]::In.ReadLine()
    if ($null -eq $inputText) {
        End-Dialog
        break
    }

    $command = $inputText.Trim()

    if ([string]::IsNullOrEmpty($command) -or $command -eq "c" -or $command -eq "continue") {
        Continue-Dialog
        continue
    }

    if ($command -eq "help") {
        Show-InteractiveHelp
        continue
    }

    if ($command -eq "quit" -or $command -eq "exit" -or $command -eq "q") {
        End-Dialog
        continue
    }

    if ($command -eq "restart") {
        Show-Node $rootNode
        continue
    }

    if ($command -eq "conditions") {
        Show-Conditions
        continue
    }

    if ($command -eq "nodes") {
        Show-Nodes
        continue
    }

    if ($command.StartsWith("set ", [System.StringComparison]::Ordinal)) {
        Set-DialogCondition $command.Substring(4).Trim() $true
        continue
    }

    if ($command.StartsWith("unset ", [System.StringComparison]::Ordinal)) {
        Set-DialogCondition $command.Substring(6).Trim() $false
        continue
    }

    if ($command.StartsWith("goto ", [System.StringComparison]::Ordinal)) {
        $nodeId = $command.Substring(5).Trim()
        if ($nodesById.ContainsKey($nodeId)) {
            Show-Node $nodesById[$nodeId]
        } else {
            Write-Host "Unknown node id: $nodeId"
        }
        continue
    }

    $optionNumber = 0
    if ([int]::TryParse($command, [ref]$optionNumber)) {
        if ($optionNumber -lt 1 -or $optionNumber -gt $script:visibleOptions.Count) {
            Write-Host "Option number out of range."
        } else {
            Show-Node $script:visibleOptions[$optionNumber - 1].TargetNode
        }
        continue
    }

    Write-Host "Unknown command: $command"
}
