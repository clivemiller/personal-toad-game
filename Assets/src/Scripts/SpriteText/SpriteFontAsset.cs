using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName = "Sprite Text/Sprite Font Asset", fileName = "SpriteFontAsset")]
public sealed class SpriteFontAsset : ScriptableObject
{
    public const string RequiredGlyphSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,`\"?!";

    [Serializable] 
    public sealed class GlyphEntry
    {
        [Tooltip("Single-character mapping for this sprite.")]
        public string character = "A";

        [Tooltip("Sprite that should render for this character.")]
        public Sprite sprite;

        [Tooltip("Optional width override in source pixels. Use -1 to read the sprite width.")]
        public float widthOverride = 0.25f;

        [Tooltip("Optional height override in source pixels. Use -1 to preserve the sprite aspect ratio from the resolved width.")]
        public float heightOverride = 0.25f;

        [Tooltip("Optional offset in source pixels. Positive Y nudges the glyph upward.")]
        public Vector2 offset;
    }

    [SerializeField]
    [Tooltip("Source font pixel height used when converting sprite sizes into UI units.")]
    private float referenceHeight = 3f;

    [SerializeField]
    [Tooltip("Width for spaces in source pixels.")]
    private float defaultSpaceWidth = 16f;

    [SerializeField]
    [Tooltip("Default gap between adjacent glyphs in source pixels.")]
    private float defaultCharacterSpacing = 2f;

    [SerializeField]
    [Tooltip("Optional fallback sprite used when a requested glyph is missing.")]
    private Sprite fallbackSprite;

    [SerializeField]
    private List<GlyphEntry> glyphs = new List<GlyphEntry>();

    private readonly Dictionary<char, SpriteGlyphData> glyphLookup = new Dictionary<char, SpriteGlyphData>();
    private bool lookupBuilt;
    private float tallestGlyphHeight;

    public float ReferenceHeight => Mathf.Max(1f, Mathf.Max(referenceHeight, tallestGlyphHeight));
    public float DefaultSpaceWidth => Mathf.Max(0f, defaultSpaceWidth);
    public float DefaultCharacterSpacing => Mathf.Max(0f, defaultCharacterSpacing);

    public bool TryGetGlyph(char character, out SpriteGlyphData glyph)
    {
        EnsureLookup();

        if (glyphLookup.TryGetValue(character, out glyph))
        {
            return true;
        }

        if (fallbackSprite != null)
        {
            glyph = new SpriteGlyphData(fallbackSprite, fallbackSprite.rect.width, fallbackSprite.rect.height, Vector2.zero);
            return true;
        }

        glyph = default;
        return false;
    }

    public bool ContainsGlyph(char character)
    {
        EnsureLookup();
        return glyphLookup.ContainsKey(character);
    }

    public float ScaleSourcePixels(float pixels, float fontSize)
    {
        return Mathf.Max(0f, pixels) * (Mathf.Max(1f, fontSize) / ReferenceHeight);
    }

    public float GetScaledSpaceWidth(float fontSize)
    {
        return ScaleSourcePixels(DefaultSpaceWidth, fontSize);
    }

    public float GetScaledCharacterSpacing(float fontSize)
    {
        return ScaleSourcePixels(DefaultCharacterSpacing, fontSize);
    }

    public string GetMissingRequiredCharacters()
    {
        EnsureLookup();

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < RequiredGlyphSet.Length; i++)
        {
            char character = RequiredGlyphSet[i];
            if (!glyphLookup.ContainsKey(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    [ContextMenu("Log Missing Required Glyphs")]
    private void LogMissingRequiredGlyphs()
    {
        string missing = GetMissingRequiredCharacters();
        if (string.IsNullOrEmpty(missing))
        {
            Debug.Log($"{name}: all required glyphs are assigned.", this);
            return;
        }

        Debug.LogWarning($"{name}: missing glyphs for '{missing}'.", this);
    }

    private void OnValidate()
    {
        lookupBuilt = false;
        glyphLookup.Clear();
    }

    private void EnsureLookup()
    {
        if (lookupBuilt)
        {
            return;
        }

        lookupBuilt = true;
        glyphLookup.Clear();
        tallestGlyphHeight = 0f;

        for (int i = 0; i < glyphs.Count; i++)
        {
            GlyphEntry entry = glyphs[i];
            if (entry == null || string.IsNullOrEmpty(entry.character) || entry.sprite == null)
            {
                continue;
            }

            char key = entry.character[0];
            float width = entry.widthOverride > 0f ? entry.widthOverride : entry.sprite.rect.width;
            float height = ResolveGlyphHeight(entry, width);

            glyphLookup[key] = new SpriteGlyphData(entry.sprite, width, height, entry.offset);
            tallestGlyphHeight = Mathf.Max(tallestGlyphHeight, height);
        }
    }

    private float ResolveGlyphHeight(GlyphEntry entry, float resolvedWidth)
    {
        if (entry.heightOverride > 0f)
        {
            return entry.heightOverride;
        }

        float spriteWidth = Mathf.Max(1f, entry.sprite.rect.width);
        float aspectScale = resolvedWidth / spriteWidth;
        return Mathf.Max(1f, entry.sprite.rect.height * aspectScale);
    }
}

public readonly struct SpriteGlyphData
{
    public SpriteGlyphData(Sprite sprite, float sourceWidth, float sourceHeight, Vector2 offset)
    {
        Sprite = sprite;
        SourceWidth = Mathf.Max(0f, sourceWidth);
        SourceHeight = Mathf.Max(0f, sourceHeight);
        Offset = offset;
    }

    public Sprite Sprite { get; }
    public float SourceWidth { get; }
    public float SourceHeight { get; }
    public Vector2 Offset { get; }
}
