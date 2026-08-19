Shader "Astrodiver/Plasma Laser Color Replace"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _InnerColor ("Inner Color (FFFFFF)", Color) = (1, 1, 1, 1)
        _MiddleColor ("Middle Color (3C3C3C)", Color) = (0.23529412, 0.23529412, 0.23529412, 1)
        _OutlineColor ("Outline Color (000000)", Color) = (0, 0, 0, 1)
        _GlowIntensity ("Glow Intensity", Float) = 2
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _InnerColor;
                half4 _MiddleColor;
                half4 _OutlineColor;
                float _GlowIntensity;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half value = source.r;

                // The sprite is imported as sRGB, so source 3C becomes about 0.045
                // after the GPU converts it to linear space. Keep this threshold
                // below that value to distinguish it from the black outline.
                half4 replacement = _OutlineColor;
                replacement = lerp(replacement, _MiddleColor, step(0.01h, value));
                replacement = lerp(replacement, _InnerColor, step(0.5h, value));

                return half4(
                    replacement.rgb * _GlowIntensity * input.color.rgb,
                    source.a * replacement.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
