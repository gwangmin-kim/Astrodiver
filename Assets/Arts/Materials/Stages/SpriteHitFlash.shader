Shader "Astrodiver/2D/Sprite Hit Flash"
{
    // 스프라이트 원본 색 x FlashColor를 가산 합성해 전체 밝기를 끌어올린다.
    // FlashColor는 밝기 배율(HDR)이며, 1을 넘는 결과는 전역 Bloom으로 발광한다.
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _FlashColor ("Flash Color (Brightness Multiplier)", Color) = (1.5, 1.5, 1.5, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off

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
                float4 _FlashColor;
                float _FlashAmount;
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

            float4 Fragment(Varyings input) : SV_Target
            {
                // 원본 텍스처 색에 비례해 더하므로, 밝아져도 타일의 명암과 무늬가 유지된다.
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return float4(source.rgb * _FlashColor.rgb,
                    source.a * input.color.a * saturate(_FlashAmount));
            }
            ENDHLSL
        }
    }
}
