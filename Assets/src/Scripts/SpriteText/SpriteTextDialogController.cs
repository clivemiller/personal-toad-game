using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class SpriteTextDialogController : BaseDialogController
{
    [Header("Dialog UI")]
    [SerializeField]
    private GameObject dialogRoot;

    [SerializeField]
    private CanvasGroup dialogCanvasGroup;

    [SerializeField]
    private SpriteTextRenderer speakerTextRenderer;

    [SerializeField]
    private SpriteTextRenderer bodyTextRenderer;

    [SerializeField]
    private GameObject continueIndicator;

    [Header("Options")]
    [SerializeField]
    private RectTransform optionsRoot;

    [SerializeField]
    private SpriteDialogOptionButton optionButtonPrefab;

    [Header("Behaviour")]
    [SerializeField]
    private bool animateBodyText = true;

    [SerializeField]
    private bool delayChoiceDisplayUntilRevealIsDone = true;

    [SerializeField]
    private bool allowMouseClickToAdvance = true;

    [SerializeField]
    private bool allowSpaceToAdvance = true;

    [SerializeField]
    private Key additionalAdvanceKey = Key.Enter;

    private readonly List<SpriteDialogOptionButton> optionPool = new List<SpriteDialogOptionButton>();

    private List<DialogOption> pendingOptions;
    private bool pendingContinue;
    private bool awaitingContinueInput;

    public override void ContinueDialog()
    {
        if (bodyTextRenderer != null && bodyTextRenderer.IsRevealing)
        {
            bodyTextRenderer.RevealAll();
            return;
        }

        awaitingContinueInput = false;
        pendingContinue = false;
        SetContinueIndicator(false);
        base.ContinueDialog();
    }

    public override void SelectOption(DialogOption option)
    {
        if (bodyTextRenderer != null && bodyTextRenderer.IsRevealing)
        {
            bodyTextRenderer.RevealAll();
            return;
        }

        awaitingContinueInput = false;
        pendingContinue = false;
        pendingOptions = null;
        SetContinueIndicator(false);
        base.SelectOption(option);
    }

    protected override void ShowDialogBox()
    {
        if (dialogRoot == null)
        {
            dialogRoot = gameObject;
        }

        dialogRoot.SetActive(true);

        if (dialogCanvasGroup != null)
        {
            dialogCanvasGroup.alpha = 1f;
            dialogCanvasGroup.interactable = true;
            dialogCanvasGroup.blocksRaycasts = true;
        }

        SetContinueIndicator(false);
    }

    protected override void HideDialogBox()
    {
        awaitingContinueInput = false;
        pendingContinue = false;
        pendingOptions = null;

        ClearOptions();
        SetContinueIndicator(false);

        if (speakerTextRenderer != null)
        {
            speakerTextRenderer.Clear();
        }

        if (bodyTextRenderer != null)
        {
            bodyTextRenderer.Clear();
        }

        if (dialogCanvasGroup != null)
        {
            dialogCanvasGroup.alpha = 0f;
            dialogCanvasGroup.interactable = false;
            dialogCanvasGroup.blocksRaycasts = false;
        }

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(false);
        }
    }

    protected override void DisplayDialogText(string speaker, string text)
    {
        awaitingContinueInput = false;
        pendingContinue = false;
        pendingOptions = null;

        ClearOptions();
        SetContinueIndicator(false);

        if (speakerTextRenderer != null)
        {
            speakerTextRenderer.SetText(speaker ?? string.Empty, false);
        }

        if (bodyTextRenderer != null)
        {
            bodyTextRenderer.SetText(text ?? string.Empty, animateBodyText);
        }
    }

    protected override void DisplayOptions(List<DialogOption> options)
    {
        awaitingContinueInput = false;
        pendingContinue = false;
        SetContinueIndicator(false);

        if (options == null || options.Count == 0)
        {
            ClearOptions();
            return;
        }

        if (delayChoiceDisplayUntilRevealIsDone && bodyTextRenderer != null && bodyTextRenderer.IsRevealing)
        {
            pendingOptions = new List<DialogOption>(options);
            ClearOptions();
            return;
        }

        pendingOptions = null;
        BuildOptions(options);
    }

    protected override void ClearOptions()
    {
        for (int i = 0; i < optionPool.Count; i++)
        {
            optionPool[i].ClearBinding();
        }
    }

    protected override void WaitForContinue()
    {
        ClearOptions();

        if (bodyTextRenderer != null && bodyTextRenderer.IsRevealing)
        {
            pendingContinue = true;
            awaitingContinueInput = false;
            SetContinueIndicator(false);
            return;
        }

        pendingContinue = false;
        awaitingContinueInput = true;
        SetContinueIndicator(true);
    }

    private void Awake()
    {
        if (dialogRoot == null)
        {
            dialogRoot = gameObject;
        }

        if (dialogCanvasGroup == null)
        {
            dialogCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (bodyTextRenderer != null)
        {
            bodyTextRenderer.RevealCompleted += HandleBodyRevealCompleted;
        }
    }

    private void OnDestroy()
    {
        if (bodyTextRenderer != null)
        {
            bodyTextRenderer.RevealCompleted -= HandleBodyRevealCompleted;
        }
    }

    private void Update()
    {
        if (currentTree == null || !ShouldAdvanceThisFrame())
        {
            return;
        }

        if (bodyTextRenderer != null && bodyTextRenderer.IsRevealing)
        {
            bodyTextRenderer.RevealAll();
            return;
        }

        if (awaitingContinueInput)
        {
            ContinueDialog();
        }
    }

    private void BuildOptions(List<DialogOption> options)
    {
        if (optionsRoot == null || optionButtonPrefab == null)
        {
            Debug.LogError("SpriteTextDialogController needs an options root and option button prefab to display dialog choices.", this);
            return;
        }

        for (int i = 0; i < options.Count; i++)
        {
            SpriteDialogOptionButton optionButton = GetOptionButton(i);
            optionButton.Bind(this, options[i]);
        }

        for (int i = options.Count; i < optionPool.Count; i++)
        {
            optionPool[i].ClearBinding();
        }
    }

    private SpriteDialogOptionButton GetOptionButton(int index)
    {
        while (optionPool.Count <= index)
        {
            SpriteDialogOptionButton instance = Instantiate(optionButtonPrefab, optionsRoot);
            instance.gameObject.name = $"{optionButtonPrefab.name}_{optionPool.Count:D2}";
            optionPool.Add(instance);
        }

        SpriteDialogOptionButton button = optionPool[index];
        if (button.transform.parent != optionsRoot)
        {
            button.transform.SetParent(optionsRoot, false);
        }

        button.gameObject.SetActive(true);
        return button;
    }

    private void HandleBodyRevealCompleted()
    {
        if (pendingOptions != null && pendingOptions.Count > 0)
        {
            List<DialogOption> options = pendingOptions;
            pendingOptions = null;
            BuildOptions(options);
            return;
        }

        if (pendingContinue)
        {
            pendingContinue = false;
            awaitingContinueInput = true;
            SetContinueIndicator(true);
        }
    }

    private void SetContinueIndicator(bool visible)
    {
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(visible);
        }
    }

    private bool ShouldAdvanceThisFrame()
    {
        if (Mouse.current != null && allowMouseClickToAdvance && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (Keyboard.current == null)
        {
            return false;
        }

        if (allowSpaceToAdvance && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }

        return Keyboard.current[additionalAdvanceKey] != null && Keyboard.current[additionalAdvanceKey].wasPressedThisFrame;
    }
}
