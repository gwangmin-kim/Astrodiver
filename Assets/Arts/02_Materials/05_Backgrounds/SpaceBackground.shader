Shader "Astrodiver/Background/Space Background"
{
    Properties
    {
        [Toggle] _UseGradient ("Use Gradient", Float) = 0
        _Color ("Space Color", Color) = (0.018, 0.026, 0.070, 1)
        _BottomColor ("Gradient Bottom", Color) = (0.018, 0.026, 0.070, 1)
        _TopColor ("Gradient Top", Color) = (0.060, 0.080, 0.180, 1)
        _CurveStrength ("Curve Strength", Range(-0.5, 0.5)) = 0.2
        _NoiseScale ("Noise Scale", Range(0.5, 12)) = 3
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.18
        _NoiseSeed ("Noise Seed", Float) = 0
        [Toggle] _UseDithering ("Use Dithering", Float) = 1
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.3
        _DitherPixelSize ("Dither Pixel Size", Range(1, 4)) = 1
        [Toggle] _UseStars ("Use Stars", Float) = 1
        _StarColor ("Star Color", Color) = (0.75, 0.85, 1, 1)
        _StarDensity ("Star Density", Range(0, 0.5)) = 0.08
        _StarCellSize ("Star Cell Size", Range(4, 64)) = 16
        _StarSize ("Star Size", Range(1, 4)) = 1.5
        _StarBrightness ("Star Brightness", Range(0, 4)) = 1
        _StarSeed ("Star Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SpaceBackground"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half _UseGradient;
                half4 _Color;
                half4 _BottomColor;
                half4 _TopColor;
                half _CurveStrength;
                half _NoiseScale;
                half _NoiseStrength;
                half _NoiseSeed;
                half _UseDithering;
                half _DitherStrength;
                half _DitherPixelSize;
                half _UseStars;
                half4 _StarColor;
                half _StarDensity;
                half _StarCellSize;
                half _StarSize;
                half _StarBrightness;
                half _StarSeed;
            CBUFFER_END

            float Hash11(float value)
            {
                value = frac(value * 0.1031);
                value *= value + 33.33;
                value *= value + value;
                return frac(value);
            }

            float SmoothNoise1D(float value)
            {
                float cell = floor(value);
                float fraction = frac(value);
                float smoothFraction = fraction * fraction * (3.0 - 2.0 * fraction);
                return lerp(Hash11(cell), Hash11(cell + 1.0), smoothFraction);
            }

            float Hash12(float2 position)
            {
                float3 hash = frac(float3(position.xyx) * 0.1031);
                hash += dot(hash, hash.yzx + 33.33);
                return frac((hash.x + hash.y) * hash.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.positionCS.xy / _ScreenParams.xy;
                half3 backgroundColor = _Color.rgb;

                if (_UseGradient >= 0.5h)
                {
                    float noiseCoordinate = screenUV.x * _NoiseScale + _NoiseSeed;
                    float broadNoise = SmoothNoise1D(noiseCoordinate);
                    float detailNoise = SmoothNoise1D(noiseCoordinate * 2.17 + 13.7);
                    float combinedNoise = broadNoise * 0.7 + detailNoise * 0.3 - 0.5;

                    float centeredX = screenUV.x * 2.0 - 1.0;
                    float curveOffset = (centeredX * centeredX - 0.35) * _CurveStrength;
                    float noiseOffset = combinedNoise * _NoiseStrength;
                    half gradientPosition = saturate(screenUV.y + curveOffset + noiseOffset);
                    backgroundColor = lerp(_BottomColor.rgb, _TopColor.rgb, gradientPosition);

                    if (_UseDithering >= 0.5h)
                    {
                        float ditherPixelSize = max((float)_DitherPixelSize, 1.0);
                        float2 ditherCell = floor(input.positionCS.xy / ditherPixelSize);
                        float dither = Hash12(ditherCell) - 0.5;
                        backgroundColor += dither * ((float)_DitherStrength / 255.0);
                    }
                }

                if (_UseStars >= 0.5h)
                {
                    float starCellSize = max((float)_StarCellSize, 1.0);
                    float2 starGrid = input.positionCS.xy / starCellSize;
                    float2 starCell = floor(starGrid);
                    float2 starCellUV = frac(starGrid);
                    float2 seededCell = starCell + (float)_StarSeed * float2(19.19, 73.73);

                    float starRandom = Hash12(seededCell);
                    float2 starPosition = 0.15 + 0.7 * float2(
                        Hash12(seededCell + float2(17.17, 3.31)),
                        Hash12(seededCell + float2(5.13, 41.71)));
                    float2 starOffsetPixels = (starCellUV - starPosition) * starCellSize;
                    float starHalfSize = max((float)_StarSize, 1.0) * 0.5;
                    float starShape = 1.0 - step(starHalfSize, max(abs(starOffsetPixels.x), abs(starOffsetPixels.y)));
                    float starExists = step(1.0 - (float)_StarDensity, starRandom);
                    float starVariation = lerp(0.55, 1.0, Hash12(seededCell + float2(29.43, 11.97)));
                    float starIntensity = starShape * starExists * starVariation * (float)_StarBrightness;

                    backgroundColor += _StarColor.rgb * starIntensity;
                }

                return half4(max(backgroundColor, 0.0h), 1.0h);
            }
            ENDHLSL
        }
    }
}
