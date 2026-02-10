using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CursorManager : MonoBehaviour
{
    public enum CursorState
    {
        Invisible,
        Pointer,
        CanLook,
        CanGrab,
        CanClick,
        IsGrabbing,
    }

    public static CursorManager Instance { get; private set; }

    [Header("Initial State")]
    [SerializeField]
    private CursorState initialState = CursorState.Pointer;

    [Header("Cursor Replacement")]
    [SerializeField]
    private SpriteRenderer cursorSpriteRenderer;

    [SerializeField]
    private Camera cursorCamera;

    [SerializeField]
    private bool hideHardwareCursor = true;

    [Header("State Sprites")]
    [SerializeField]
    private Sprite pointerSprite;

    [SerializeField]
    private Sprite canLookSprite;

    [SerializeField]
    private Sprite canGrabSprite;

    [SerializeField]
    private Sprite canClickSprite;

    [SerializeField]
    private Sprite isGrabbingSprite;

    [Header("Can Click Rotation")]
    [SerializeField]
    private float canClickRotationSpeedDegreesPerSecond = 180f;

    [Header("Can Click Scale")]
    [SerializeField]
    private float canClickScaleMultiplier = 2f;

    public CursorState CurrentState { get; private set; }

    private Vector3 baseCursorLocalScale = Vector3.one;

    private void Update()
    {
        UpdateCursorPosition();

        if (CurrentState != CursorState.CanClick)
        {
            return;
        }

        float deltaRotation = canClickRotationSpeedDegreesPerSecond * Time.unscaledDeltaTime;

        if (cursorSpriteRenderer == null)
        {
            return;
        }

        cursorSpriteRenderer.transform.Rotate(0f, 0f, deltaRotation);
    }

    private void UpdateCursorPosition()
    {
        if (cursorSpriteRenderer == null)
        {
            return;
        }

        if (cursorCamera == null)
        {
            cursorCamera = Camera.main;
        }

        if (cursorCamera == null)
        {
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 cursorScreenPos = cursorCamera.WorldToScreenPoint(cursorSpriteRenderer.transform.position);

        Vector3 screenPoint = new Vector3(mouseScreen.x, mouseScreen.y, cursorScreenPos.z);
        Vector3 worldPoint = cursorCamera.ScreenToWorldPoint(screenPoint);

        cursorSpriteRenderer.transform.position = worldPoint;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (hideHardwareCursor)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = false;
        }

        if (cursorSpriteRenderer != null)
        {
            baseCursorLocalScale = cursorSpriteRenderer.transform.localScale;
        }

        ApplyState(initialState);
    }

    /// <summary>
    /// Switches the cursor state.
    /// </summary>
    public void Switch(CursorState newState)
    {
        ApplyState(newState);
    }

    private void ApplyState(CursorState state)
    {
        CurrentState = state;

        ResetRotation();

        Sprite nextSprite = GetSpriteForState(state);
        bool visible = state != CursorState.Invisible && nextSprite != null;

        ApplySprite(nextSprite, visible);
        ApplyScale(state);

        switch (state)
        {
            case CursorState.Invisible:
            case CursorState.Pointer:
            case CursorState.CanLook:
            case CursorState.CanGrab:
            case CursorState.CanClick:
            case CursorState.IsGrabbing:
                break;
        }
    }

    private void ApplyScale(CursorState state)
    {
        if (cursorSpriteRenderer == null)
        {
            return;
        }

        float multiplier = state == CursorState.CanClick ? Mathf.Max(0f, canClickScaleMultiplier) : 1f;
        cursorSpriteRenderer.transform.localScale = baseCursorLocalScale * multiplier;
    }

    private Sprite GetSpriteForState(CursorState state)
    {
        switch (state)
        {
            case CursorState.Pointer:
                return pointerSprite;
            case CursorState.CanLook:
                return canLookSprite;
            case CursorState.CanGrab:
                return canGrabSprite;
            case CursorState.CanClick:
                return canClickSprite;
            case CursorState.IsGrabbing:
                return isGrabbingSprite;
            case CursorState.Invisible:
            default:
                return null;
        }
    }

    private void ApplySprite(Sprite sprite, bool visible)
    {
        if (cursorSpriteRenderer == null)
        {
            return;
        }

        cursorSpriteRenderer.sprite = sprite;
        cursorSpriteRenderer.gameObject.SetActive(visible);
    }

    private void ResetRotation()
    {
        if (cursorSpriteRenderer == null)
        {
            return;
        }

        cursorSpriteRenderer.transform.localRotation = Quaternion.identity;
    }
}
