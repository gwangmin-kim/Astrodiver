Shader "Astrodiver/2D/Sprite Lit 8-Direction Outline"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width (Screen Pixels)", Range(0, 16)) = 2
        [HideInInspector] _OutlineEnabled("Outline Enabled", Float) = 0
        [HideInInspector] _SpriteUVMinMax("Sprite UV Min Max", Vector) = (0, 0, 1, 1)
        [HideInInspector] _SpriteLocalSize("Sprite Local Size", Vector) = (1, 1, 0, 0)
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Kept for compatibility with SpriteRenderer and the built-in Sprite-Lit shader.
        [HideInInspector] _Color("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineEnabled;
                float4 _SpriteUVMinMax;
                float4 _SpriteLocalSize;
            CBUFFER_END

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                if (_OutlineEnabled >= 0.5)
                {
                    float4 positionCS = TransformObjectToHClip(input.positionOS);
                    float2 pixelPerLocalUnit = float2(
                        length((TransformObjectToHClip(input.positionOS + float3(1, 0, 0)).xy /
                            TransformObjectToHClip(input.positionOS + float3(1, 0, 0)).w -
                            positionCS.xy / positionCS.w) * 0.5 * _ScreenParams.xy),
                        length((TransformObjectToHClip(input.positionOS + float3(0, 1, 0)).xy /
                            TransformObjectToHClip(input.positionOS + float3(0, 1, 0)).w -
                            positionCS.xy / positionCS.w) * 0.5 * _ScreenParams.xy));
                    float2 localExpansion = _OutlineWidth / max(pixelPerLocalUnit, 0.0001);
                    float2 uvCenter = (_SpriteUVMinMax.xy + _SpriteUVMinMax.zw) * 0.5;
                    float2 uvDirection = sign(input.uv - uvCenter);
                    float2 uvExpansion = localExpansion /
                        max(_SpriteLocalSize.xy, 0.0001) * (_SpriteUVMinMax.zw - _SpriteUVMinMax.xy);

                    // Expand the geometry itself so sprites without transparent edge
                    // pixels (for example, the square Gate sprite) can draw outside
                    // their original quad. UVs are expanded by the matching amount.
                    input.positionOS.xy += uvDirection * unity_SpriteProps.xy * localExpansion;
                    input.uv += uvDirection * uvExpansion;
                }

                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half SampleSpriteAlpha(float2 uv)
            {
                bool insideSprite = all(uv >= _SpriteUVMinMax.xy) && all(uv <= _SpriteUVMinMax.zw);
                return insideSprite ? SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a : 0.0h;
            }

            half4 SampleSprite(float2 uv)
            {
                bool insideSprite = all(uv >= _SpriteUVMinMax.xy) && all(uv <= _SpriteUVMinMax.zw);
                return insideSprite ? SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) : 0.0h;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                // All inactive interactables return before the eight neighbour samples.
                if (_OutlineEnabled < 0.5)
                {
                    return CommonLitFragment(input, input.color);
                }

                // ddx/ddy convert a screen-pixel movement into UV movement. This keeps
                // the outline width fixed in screen pixels when the Transform is scaled.
                const float2 x = ddx(input.uv) * _OutlineWidth;
                const float2 y = ddy(input.uv) * _OutlineWidth;
                const float2 diagonalX = x * 0.70710678;
                const float2 diagonalY = y * 0.70710678;

                const half centerAlpha = SampleSpriteAlpha(input.uv);
                half neighbourAlpha = 0;
                neighbourAlpha = max(neighbourAlpha, SampleSpriteAlpha(input.uv + x));
                neighbourAlpha = max(neighbourAlpha, SampleSpriteAlpha(input.uv - x));
                neighbourAlpha = max(neighbourAlpha, SampleSpriteAlpha(input.uv + y));
                neighbourAlpha = max(neighbourAlpha, SampleSpriteAlpha(input.uv - y));
                neighbourAlpha = max(neighbourAlpha, SampleSpriteAlpha(input.uv + diagonalX + diagonalY));
                neighbourAlpha = max(neighbourAlpha, SampleSpriteAlpha(input.uv + diagonalX - diagonalY));
                neighbourAlpha = max(neighbourAlpha, SampleSpriteAlpha(input.uv - diagonalX + diagonalY));
                neighbourAlpha = max(neighbourAlpha, SampleSpriteAlpha(input.uv - diagonalX - diagonalY));

                const half4 sprite = input.color * SampleSprite(input.uv);
                const half outlineAlpha = saturate(neighbourAlpha * input.color.a) * (1.0h - centerAlpha);
                const half finalAlpha = max(sprite.a, outlineAlpha * _OutlineColor.a);
                const half3 finalAlbedo = lerp(_OutlineColor.rgb, sprite.rgb, centerAlpha);

                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(finalAlbedo, finalAlpha, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                    surfaceData.normalWS = input.normalWS;
                #endif

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        // Match the extra passes provided by Sprite-Lit-Default so normal-map rendering
        // and the regular forward fallback keep working with this material.
        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineEnabled;
                float4 _SpriteUVMinMax;
                float4 _SpriteLocalSize;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonNormalsVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                SetUpSpriteInstanceProperties();
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "Transparent" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
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
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineEnabled;
                float4 _SpriteUVMinMax;
                float4 _SpriteLocalSize;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
