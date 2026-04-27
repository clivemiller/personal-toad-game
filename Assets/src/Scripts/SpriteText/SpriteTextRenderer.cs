using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SpriteTextRenderer : MonoBehaviour
{
    private enum TokenType
    {
        Word,
        Space,
        NewLine,
    }

    private enum InlineEffectType
    {
        None,
        Jitter,
        Wave,
    }

    private enum VerticalAlignMode
    {
        Bottom,
        Top,
    }

    [Serializable]
    private struct Padding
    {
        public float left;
        public float right;
        public float top;
        public float bottom;
    }

    [Serializable]
    private struct EffectSettings
    {
        public float amplitude;
        public float speed;
        public float rotation;
    }

    private sealed class StyledCharacter
    {
        public StyledCharacter(char character, InlineEffectType effect)
        {
            Character = character;
            Effect = effect;
        }

        public char Character { get; }
        public InlineEffectType Effect { get; }
    }

    private sealed class Token
    {
        public Token(TokenType type)
        {
            Type = type;
            Characters = new List<StyledCharacter>();
        }

        public TokenType Type { get; }
        public List<StyledCharacter> Characters { get; }
    }

    private struct RenderedGlyph
    {
        public SpriteTextGlyph View;
        public InlineEffectType Effect;
        public float Phase;
    }

    [Header("Font")]
    [SerializeField]
    private SpriteFontAsset fontAsset;

    [SerializeField]
    [Tooltip("Optional child rect that hosts glyph instances. Defaults to this rect transform.")]
    private RectTransform contentRoot;

    [SerializeField]
    private SpriteTextGlyph glyphPrefab;

    [Header("Source Text")]
    [SerializeField]
    [Tooltip("Optional inspector-authored text used when no script has set text yet.")]
    [TextArea(3, 8)]
    private string serializedText = string.Empty;

    [SerializeField]
    [Tooltip("When enabled, the serialized text field is used as the starting text for this renderer.")]
    private bool useSerializedTextOnEnable = true;

    [Header("Layout")]
    [SerializeField]
    [Min(1f)]
    private float fontSize = 32f;

    [SerializeField]
    [Tooltip("Override in source pixels. Use -1 to use the value from the font asset.")]
    private float characterSpacingOverride = -1f;

    [SerializeField]
    [Tooltip("Extra line spacing in source pixels.")]
    private float additionalLineSpacing = 6f;

    [SerializeField]
    [Tooltip("How glyph sprites are aligned within each line box.")]
    private VerticalAlignMode verticalAlign = VerticalAlignMode.Bottom;

    [SerializeField]
    private Padding padding = new Padding
    {
        left = 12f,
        right = 12f,
        top = 12f,
        bottom = 12f,
    };

    [SerializeField]
    private Color glyphColor = Color.white;

    [SerializeField]
    private bool autoResizeHeight = true;

    [Header("Reveal Animation")]
    [SerializeField]
    private bool animateOnSet = true;

    [SerializeField]
    [Min(0f)]
    private float revealCharactersPerSecond = 30f;

    [SerializeField]
    private bool useUnscaledTime = true;

    [Header("Inline Effects")]
    [SerializeField]
    private EffectSettings jitterSettings = new EffectSettings
    {
        amplitude = 1.5f,
        speed = 28f,
        rotation = 2f,
    };

    [SerializeField]
    private EffectSettings waveSettings = new EffectSettings
    {
        amplitude = 2f,
        speed = 10f,
        rotation = 0f,
    };

    [Header("Events")]
    [SerializeField]
    private UnityEvent onRevealCompleted = new UnityEvent();

    private readonly List<SpriteTextGlyph> glyphPool = new List<SpriteTextGlyph>();
    private readonly List<RenderedGlyph> activeGlyphs = new List<RenderedGlyph>();

    private RectTransform hostRectTransform;
    private string rawText = string.Empty;
    private float revealProgress;
    private int visibleGlyphCount;
    private bool hasWarnedAboutMissingCanvas;
    private bool hasWarnedAboutTinyFontSize;

    public event Action RevealCompleted;

    public bool IsRevealing { get; private set; }
    public bool IsRevealComplete => !IsRevealing && visibleGlyphCount >= activeGlyphs.Count;
    public string Text => rawText;
    public string SerializedText => serializedText;

    public void SetText(string text)
    {
        SetText(text, animateOnSet);
    }

    public void SetText(string text, bool animate)
    {
        rawText = text ?? string.Empty;
        Rebuild(animate);
    }

    public void Clear()
    {
        rawText = string.Empty;
        Rebuild(false);
    }

    public void SetSerializedText(string text, bool applyImmediately = true)
    {
        serializedText = text ?? string.Empty;

        if (applyImmediately)
        {
            rawText = serializedText;
            Rebuild(animateOnSet);
        }
    }

    public void RevealAll()
    {
        bool wasRevealing = IsRevealing;

        IsRevealing = false;
        revealProgress = activeGlyphs.Count;
        visibleGlyphCount = activeGlyphs.Count;
        ApplyVisibleGlyphCount(visibleGlyphCount);
        UpdateAnimatedGlyphs();

        if (wasRevealing)
        {
            NotifyRevealCompleted();
        }
    }

    public void SetTextBoxSize(Vector2 size)
    {
        RectTransform rect = GetHostRectTransform();
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        Rebuild(IsRevealing);
    }

    private void Awake()
    {
        EnsureTransforms();

        if (useSerializedTextOnEnable && string.IsNullOrEmpty(rawText))
        {
            rawText = serializedText ?? string.Empty;
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (useSerializedTextOnEnable && string.IsNullOrEmpty(rawText))
        {
            rawText = serializedText ?? string.Empty;
        }

        if (!string.IsNullOrEmpty(rawText))
        {
            Rebuild(false);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
        {
            return;
        }

        Rebuild(false);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
        {
            return;
        }

        Rebuild(false);
    }

    private void Update()
    {
        if (IsRevealing)
        {
            UpdateRevealAnimation();
        }

        UpdateAnimatedGlyphs();
    }

    private void Rebuild(bool animate)
    {
        EnsureTransforms();
        activeGlyphs.Clear();

        ValidateRendererSetup();

        if (fontAsset == null || glyphPrefab == null)
        {
            ReleaseUnusedGlyphs(0);
            visibleGlyphCount = 0;
            IsRevealing = false;
            UpdateContentRoot(0f, 0f);
            return;
        }

        List<Token> tokens = Tokenize(ParseStyledCharacters(rawText));
        float contentWidth = GetAvailableContentWidth();
        float lineStep = fontSize + fontAsset.ScaleSourcePixels(additionalLineSpacing, fontSize);
        float x = 0f;
        float y = 0f;
        float widestLine = 0f;
        int poolIndex = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            Token token = tokens[i];

            if (token.Type == TokenType.NewLine)
            {
                widestLine = Mathf.Max(widestLine, x);
                x = 0f;
                y += lineStep;
                continue;
            }

            if (token.Type == TokenType.Space)
            {
                if (x <= 0f)
                {
                    continue;
                }

                float tokenWidth = MeasureSpaceToken(token);
                if (x + tokenWidth > contentWidth)
                {
                    widestLine = Mathf.Max(widestLine, x);
                    x = 0f;
                    y += lineStep;
                    continue;
                }

                x += tokenWidth;
                widestLine = Mathf.Max(widestLine, x);
                continue;
            }

            float wordWidth = MeasureWordToken(token);
            if (x > 0f && x + wordWidth > contentWidth)
            {
                widestLine = Mathf.Max(widestLine, x);
                x = 0f;
                y += lineStep;
            }

            for (int charIndex = 0; charIndex < token.Characters.Count; charIndex++)
            {
                StyledCharacter styledCharacter = token.Characters[charIndex];
                float glyphAdvance = GetCharacterAdvance(styledCharacter.Character);
                float spacingBefore = charIndex > 0 ? GetCharacterSpacing() : 0f;

                if (x > 0f && x + spacingBefore + glyphAdvance > contentWidth)
                {
                    widestLine = Mathf.Max(widestLine, x);
                    x = 0f;
                    y += lineStep;
                    spacingBefore = 0f;
                }

                x += spacingBefore;

                if (fontAsset.TryGetGlyph(styledCharacter.Character, out SpriteGlyphData glyphData) && glyphData.Sprite != null)
                {
                    SpriteTextGlyph glyphView = GetGlyphFromPool(poolIndex++);
                    Vector2 size = GetGlyphSize(glyphData);
                    Vector2 offset = GetGlyphOffset(glyphData);
                    Vector2 anchoredPosition = new Vector2(
                        x + offset.x,
                        GetAlignedGlyphY(y, size.y) + offset.y);

                    glyphView.Configure(glyphData.Sprite, anchoredPosition, size, glyphColor);

                    activeGlyphs.Add(new RenderedGlyph
                    {
                        View = glyphView,
                        Effect = styledCharacter.Effect,
                        Phase = activeGlyphs.Count * 0.41f,
                    });
                }

                x += glyphAdvance;
                widestLine = Mathf.Max(widestLine, x);
            }
        }

        ReleaseUnusedGlyphs(poolIndex);

        float contentHeight = string.IsNullOrEmpty(rawText) ? 0f : y + fontSize;
        UpdateContentRoot(widestLine, contentHeight);
        StartReveal(animate);
    }

    private void ValidateRendererSetup()
    {
        if (!hasWarnedAboutMissingCanvas && GetComponentInParent<Canvas>() == null)
        {
            hasWarnedAboutMissingCanvas = true;
            Debug.LogWarning(
                "SpriteTextRenderer is not under a Canvas. The spawned glyph Images will not render until this object is parented under a Canvas.",
                this);
        }

        if (!hasWarnedAboutTinyFontSize && fontSize <= 2f)
        {
            hasWarnedAboutTinyFontSize = true;
            Debug.LogWarning(
                $"SpriteTextRenderer fontSize is set to {fontSize}. This scales glyph sprites down to near-invisible size. Use a value closer to the font asset reference height, such as 32.",
                this);
        }
    }

    private void StartReveal(bool animate)
    {
        if (!animate || activeGlyphs.Count == 0 || revealCharactersPerSecond <= 0f)
        {
            IsRevealing = false;
            revealProgress = activeGlyphs.Count;
            visibleGlyphCount = activeGlyphs.Count;
            ApplyVisibleGlyphCount(visibleGlyphCount);
            UpdateAnimatedGlyphs();
            return;
        }

        IsRevealing = true;
        revealProgress = 0f;
        visibleGlyphCount = 0;
        ApplyVisibleGlyphCount(0);
        UpdateAnimatedGlyphs();
    }

    private void UpdateRevealAnimation()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        revealProgress += deltaTime * revealCharactersPerSecond;

        int nextVisibleCount = Mathf.Clamp(Mathf.FloorToInt(revealProgress), 0, activeGlyphs.Count);
        if (nextVisibleCount != visibleGlyphCount)
        {
            visibleGlyphCount = nextVisibleCount;
            ApplyVisibleGlyphCount(visibleGlyphCount);
        }

        if (visibleGlyphCount >= activeGlyphs.Count)
        {
            IsRevealing = false;
            NotifyRevealCompleted();
        }
    }

    private void NotifyRevealCompleted()
    {
        RevealCompleted?.Invoke();
        onRevealCompleted?.Invoke();
    }

    private void ApplyVisibleGlyphCount(int count)
    {
        for (int i = 0; i < activeGlyphs.Count; i++)
        {
            activeGlyphs[i].View.SetVisible(i < count);
        }
    }

    private void UpdateAnimatedGlyphs()
    {
        if (activeGlyphs.Count == 0)
        {
            return;
        }

        float now = useUnscaledTime ? Time.unscaledTime : Time.time;

        for (int i = 0; i < activeGlyphs.Count; i++)
        {
            RenderedGlyph glyph = activeGlyphs[i];
            if (!glyph.View.IsVisible)
            {
                glyph.View.ResetAnimatedOffset();
                continue;
            }

            Vector2 offset = Vector2.zero;
            float rotation = 0f;

            switch (glyph.Effect)
            {
                case InlineEffectType.Jitter:
                    ApplyJitterEffect(glyph.Phase, now, out offset, out rotation);
                    break;
                case InlineEffectType.Wave:
                    ApplyWaveEffect(glyph.Phase, now, out offset, out rotation);
                    break;
                default:
                    break;
            }

            glyph.View.SetAnimatedOffset(offset, rotation);
        }
    }

    private void ApplyJitterEffect(float phase, float now, out Vector2 offset, out float rotation)
    {
        float sampleTime = now * jitterSettings.speed;
        float xNoise = (Mathf.PerlinNoise(phase, sampleTime) - 0.5f) * 2f;
        float yNoise = (Mathf.PerlinNoise(sampleTime, phase) - 0.5f) * 2f;
        float rotationNoise = (Mathf.PerlinNoise(phase * 0.37f, sampleTime * 0.63f) - 0.5f) * 2f;

        offset = new Vector2(xNoise, yNoise) * jitterSettings.amplitude;
        rotation = rotationNoise * jitterSettings.rotation;
    }

    private void ApplyWaveEffect(float phase, float now, out Vector2 offset, out float rotation)
    {
        float wave = Mathf.Sin((now * waveSettings.speed) + phase) * waveSettings.amplitude;
        offset = new Vector2(0f, wave);
        rotation = Mathf.Cos((now * waveSettings.speed * 0.5f) + phase) * waveSettings.rotation;
    }

    private float MeasureWordToken(Token token)
    {
        float width = 0f;
        float spacing = GetCharacterSpacing();

        for (int i = 0; i < token.Characters.Count; i++)
        {
            width += GetCharacterAdvance(token.Characters[i].Character);
            if (i < token.Characters.Count - 1)
            {
                width += spacing;
            }
        }

        return width;
    }

    private float MeasureSpaceToken(Token token)
    {
        return token.Characters.Count * fontAsset.GetScaledSpaceWidth(fontSize);
    }

    private float GetCharacterAdvance(char character)
    {
        if (character == ' ')
        {
            return fontAsset.GetScaledSpaceWidth(fontSize);
        }

        if (fontAsset.TryGetGlyph(character, out SpriteGlyphData glyphData))
        {
            return fontAsset.ScaleSourcePixels(glyphData.SourceWidth, fontSize);
        }

        return fontAsset.GetScaledSpaceWidth(fontSize);
    }

    private Vector2 GetGlyphSize(SpriteGlyphData glyphData)
    {
        float scale = fontAsset.ScaleSourcePixels(1f, fontSize);
        return new Vector2(glyphData.SourceWidth * scale, glyphData.SourceHeight * scale);
    }

    private Vector2 GetGlyphOffset(SpriteGlyphData glyphData)
    {
        return new Vector2(
            fontAsset.ScaleSourcePixels(glyphData.Offset.x, fontSize),
            fontAsset.ScaleSourcePixels(glyphData.Offset.y, fontSize));
    }

    private float GetAlignedGlyphY(float lineOffset, float glyphHeight)
    {
        switch (verticalAlign)
        {
            case VerticalAlignMode.Top:
                return -lineOffset;
            case VerticalAlignMode.Bottom:
            default:
                return -lineOffset - fontSize + glyphHeight;
        }
    }

    private float GetCharacterSpacing()
    {
        if (characterSpacingOverride >= 0f)
        {
            return fontAsset.ScaleSourcePixels(characterSpacingOverride, fontSize);
        }

        return fontAsset.GetScaledCharacterSpacing(fontSize);
    }

    private float GetAvailableContentWidth()
    {
        float width = GetHostRectTransform().rect.width - padding.left - padding.right;
        return Mathf.Max(1f, width);
    }

    private void UpdateContentRoot(float contentWidth, float contentHeight)
    {
        RectTransform host = GetHostRectTransform();
        RectTransform content = GetContentTransform();
        float resolvedWidth = Mathf.Max(0f, contentWidth);
        float resolvedHeight = Mathf.Max(0f, contentHeight);

        if (content != host)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = new Vector2(padding.left, -padding.top);
            content.sizeDelta = new Vector2(resolvedWidth, resolvedHeight);
        }

        if (!autoResizeHeight)
        {
            return;
        }

        float targetHeight = resolvedHeight + padding.top + padding.bottom;
        if (!Mathf.Approximately(host.rect.height, targetHeight))
        {
            host.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        }
    }

    private SpriteTextGlyph GetGlyphFromPool(int index)
    {
        EnsureTransforms();

        while (glyphPool.Count <= index)
        {
            SpriteTextGlyph instance = Instantiate(glyphPrefab, GetContentTransform());
            instance.gameObject.name = $"{glyphPrefab.name}_{glyphPool.Count:D3}";
            glyphPool.Add(instance);
        }

        SpriteTextGlyph glyph = glyphPool[index];
        if (glyph.transform.parent != GetContentTransform())
        {
            glyph.transform.SetParent(GetContentTransform(), false);
        }

        glyph.gameObject.SetActive(true);
        return glyph;
    }

    private void ReleaseUnusedGlyphs(int usedCount)
    {
        for (int i = 0; i < glyphPool.Count; i++)
        {
            bool shouldBeActive = i < usedCount;
            if (glyphPool[i].gameObject.activeSelf != shouldBeActive)
            {
                glyphPool[i].gameObject.SetActive(shouldBeActive);
            }
        }
    }

    private List<StyledCharacter> ParseStyledCharacters(string text)
    {
        List<StyledCharacter> characters = new List<StyledCharacter>(text.Length);
        List<InlineEffectType> effectStack = new List<InlineEffectType>();
        InlineEffectType currentEffect = InlineEffectType.None;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '[' && TryParseEffectTag(text, i, out InlineEffectType tagEffect, out bool closingTag, out int consumedLength))
            {
                if (closingTag)
                {
                    for (int stackIndex = effectStack.Count - 1; stackIndex >= 0; stackIndex--)
                    {
                        if (effectStack[stackIndex] == tagEffect)
                        {
                            effectStack.RemoveAt(stackIndex);
                            break;
                        }
                    }
                }
                else
                {
                    effectStack.Add(tagEffect);
                }

                currentEffect = effectStack.Count > 0 ? effectStack[effectStack.Count - 1] : InlineEffectType.None;
                i += consumedLength - 1;
                continue;
            }

            char character = text[i];
            if (character == '\r')
            {
                continue;
            }

            if (character == '\t')
            {
                characters.Add(new StyledCharacter(' ', currentEffect));
                characters.Add(new StyledCharacter(' ', currentEffect));
                characters.Add(new StyledCharacter(' ', currentEffect));
                characters.Add(new StyledCharacter(' ', currentEffect));
                continue;
            }

            characters.Add(new StyledCharacter(character, currentEffect));
        }

        return characters;
    }

    private List<Token> Tokenize(List<StyledCharacter> characters)
    {
        List<Token> tokens = new List<Token>();
        Token currentWord = null;
        Token currentSpace = null;

        for (int i = 0; i < characters.Count; i++)
        {
            StyledCharacter character = characters[i];

            if (character.Character == '\n')
            {
                FlushToken(tokens, ref currentWord);
                FlushToken(tokens, ref currentSpace);
                tokens.Add(new Token(TokenType.NewLine));
                continue;
            }

            if (character.Character == ' ')
            {
                FlushToken(tokens, ref currentWord);
                if (currentSpace == null)
                {
                    currentSpace = new Token(TokenType.Space);
                }

                currentSpace.Characters.Add(character);
                continue;
            }

            FlushToken(tokens, ref currentSpace);
            if (currentWord == null)
            {
                currentWord = new Token(TokenType.Word);
            }

            currentWord.Characters.Add(character);
        }

        FlushToken(tokens, ref currentWord);
        FlushToken(tokens, ref currentSpace);

        return tokens;
    }

    private void FlushToken(List<Token> tokens, ref Token token)
    {
        if (token == null)
        {
            return;
        }

        if (token.Characters.Count > 0)
        {
            tokens.Add(token);
        }

        token = null;
    }

    private bool TryParseEffectTag(string text, int startIndex, out InlineEffectType effect, out bool closingTag, out int consumedLength)
    {
        effect = InlineEffectType.None;
        closingTag = false;
        consumedLength = 0;

        int endIndex = text.IndexOf(']', startIndex + 1);
        if (endIndex < 0)
        {
            return false;
        }

        string tag = text.Substring(startIndex + 1, endIndex - startIndex - 1).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(tag))
        {
            return false;
        }

        if (tag[0] == '/')
        {
            closingTag = true;
            tag = tag.Substring(1);
        }

        switch (tag)
        {
            case "jitter":
            case "shake":
                effect = InlineEffectType.Jitter;
                break;
            case "wave":
                effect = InlineEffectType.Wave;
                break;
            default:
                return false;
        }

        consumedLength = (endIndex - startIndex) + 1;
        return true;
    }

    private void EnsureTransforms()
    {
        if (hostRectTransform == null)
        {
            hostRectTransform = GetComponent<RectTransform>();
        }

        if (contentRoot != null)
        {
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(0f, 1f);
            contentRoot.pivot = new Vector2(0f, 1f);
        }
    }

    private RectTransform GetHostRectTransform()
    {
        EnsureTransforms();
        return hostRectTransform;
    }

    private RectTransform GetContentTransform()
    {
        EnsureTransforms();
        return contentRoot != null ? contentRoot : hostRectTransform;
    }
}
