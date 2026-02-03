Shader "Toads/GrayscaleSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GrayscaleAmount ("Grayscale Amount", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        HLSLINCLUDE
        #pragma vertex vert
        #pragma fragment frag

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _GrayscaleAmount;
        CBUFFER_END

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = IN.uv;
            OUT.color = IN.color * _Color;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

            // Luminance (Rec. 601)
            half l = dot(c.rgb, half3(0.299h, 0.587h, 0.114h));
            c.rgb = lerp(c.rgb, l.xxx, saturate(_GrayscaleAmount));

            // Premultiply alpha (SpriteRenderer expects this blend)
            c.rgb *= c.a;
            return c;
        }
        ENDHLSL

        Pass
        {
            Name "Sprite2D"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            ENDHLSL
        }

        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            ENDHLSL
        }
    }
}
