using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SpriteDialogOptionButton : MonoBehaviour
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private SpriteTextRenderer labelRenderer;

    [SerializeField]
    private bool animateLabelOnBind;

    private BaseDialogController owner;
    private DialogOption currentOption;

    public void Bind(BaseDialogController controller, DialogOption option)
    {
        owner = controller;
        currentOption = option;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (labelRenderer == null)
        {
            labelRenderer = GetComponentInChildren<SpriteTextRenderer>(true);
        }

        if (labelRenderer != null)
        {
            labelRenderer.SetText(option != null ? option.text : string.Empty, animateLabelOnBind);
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            button.interactable = option != null;
        }

        gameObject.SetActive(option != null);
    }

    public void ClearBinding()
    {
        owner = null;
        currentOption = null;

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        if (labelRenderer != null)
        {
            labelRenderer.Clear();
        }

        gameObject.SetActive(false);
    }

    private void Reset()
    {
        button = GetComponent<Button>();
        labelRenderer = GetComponentInChildren<SpriteTextRenderer>(true);
    }

    private void HandleClick()
    {
        if (owner == null || currentOption == null)
        {
            return;
        }

        owner.SelectOption(currentOption);
    }
}
