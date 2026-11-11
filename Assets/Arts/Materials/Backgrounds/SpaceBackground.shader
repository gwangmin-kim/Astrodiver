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
        [Header(Nebula)]
        [Toggle] _UseNebula ("Use Nebula", Float) = 1
        _NebulaColor ("Nebula Color", Color) = (0.12, 0.16, 0.38, 1)
        _NebulaScale ("Nebula Scale", Range(0.5, 6)) = 1.6
        _NebulaStrength ("Nebula Strength", Range(0, 0.5)) = 0.08
        _NebulaCoverage ("Nebula Coverage", Range(0.2, 0.8)) = 0.52
        _NebulaSeed ("Nebula Seed", Float) = 7
        _NebulaParallax ("Nebula Parallax", Range(0, 0.25)) = 0.015
        [Header(Star Layers)]
        [Toggle] _UseStars ("Use Stars", Float) = 1
        _StarColor ("Star Color", Color) = (0.75, 0.85, 1, 1)
        [Toggle] _UseStarParallax ("Use Star Parallax", Float) = 1
        _StarParallaxStrength ("Parallax Strength", Range(0, 2)) = 1
        [Header(Far Stars)]
        _FarStarDensity ("Far Density", Range(0, 0.5)) = 0.055
        _FarStarCellSize ("Far Cell Size", Range(4, 64)) = 14
        _FarStarSize ("Far Size", Range(1, 4)) = 1
        _FarStarBrightness ("Far Brightness", Range(0, 4)) = 0.35
        _FarStarSeed ("Far Seed", Float) = 11
        _FarStarParallax ("Far Parallax", Range(0, 1)) = 0.03
        [Header(Middle Stars)]
        _StarDensity ("Middle Density", Range(0, 0.5)) = 0.06
        _StarCellSize ("Middle Cell Size", Range(4, 64)) = 18
        _StarSize ("Middle Size", Range(1, 4)) = 1.5
        _StarBrightness ("Middle Brightness", Range(0, 4)) = 0.65
        _StarSeed ("Middle Seed", Float) = 29
        _MidStarParallax ("Middle Parallax", Range(0, 1)) = 0.1
        [Header(Near Stars)]
        _NearStarDensity ("Near Density", Range(0, 0.5)) = 0.025
        _NearStarCellSize ("Near Cell Size", Range(4, 64)) = 24
        _NearStarSize ("Near Size", Range(1, 4)) = 2
        _NearStarBrightness ("Near Brightness", Range(0, 4)) = 1
        _NearStarSeed ("Near Seed", Float) = 47
        _NearStarParallax ("Near Parallax", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            // Render before 2D sprite layers. Keeping this out of the transparent
            // queue prevents it from overpainting objects on lower sorting layers.
            "Queue" = "Background"
            "RenderType" = "Background"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SpaceBackground"
            Cull Off
            ZWrite Off
            ZTest LEqual
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
                half _UseNebula;
                half4 _NebulaColor;
                half _NebulaScale;
                half _NebulaStrength;
                half _NebulaCoverage;
                half _NebulaSeed;
                half _NebulaParallax;
                half _UseStars;
                half4 _StarColor;
                half _UseStarParallax;
                half _StarParallaxStrength;
                half _FarStarDensity;
                half _FarStarCellSize;
                half _FarStarSize;
                half _FarStarBrightness;
                half _FarStarSeed;
                half _FarStarParallax;
                half _StarDensity;
                half _StarCellSize;
                half _StarSize;
                half _StarBrightness;
                half _StarSeed;
                half _MidStarParallax;
                half _NearStarDensity;
                half _NearStarCellSize;
                half _NearStarSize;
                half _NearStarBrightness;
                half _NearStarSeed;
                half _NearStarParallax;
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

            float SmoothNoise2D(float2 value)
            {
                float2 cell = floor(value);
                float2 fraction = frac(value);
                float2 smoothFraction = fraction * fraction * (3.0 - 2.0 * fraction);

                float bottom = lerp(Hash12(cell), Hash12(cell + float2(1.0, 0.0)), smoothFraction.x);
                float top = lerp(Hash12(cell + float2(0.0, 1.0)), Hash12(cell + 1.0), smoothFraction.x);
                return lerp(bottom, top, smoothFraction.y);
            }

            float SampleStarLayer(
                float2 pixelPosition,
                float cellSize,
                float density,
                float starSize,
                float brightness,
                float seed)
            {
                cellSize = max(cellSize, 1.0);
                float2 starGrid = pixelPosition / cellSize;
                float2 starCell = floor(starGrid);
                float2 starCellUV = frac(starGrid);
                float2 seededCell = starCell + seed * float2(19.19, 73.73);

                float starRandom = Hash12(seededCell);
                float2 starPosition = 0.15 + 0.7 * float2(
                    Hash12(seededCell + float2(17.17, 3.31)),
                    Hash12(seededCell + float2(5.13, 41.71)));
                float2 starOffsetPixels = (starCellUV - starPosition) * cellSize;
                float starHalfSize = max(starSize, 1.0) * 0.5;
                float starShape = 1.0 - step(starHalfSize, max(abs(starOffsetPixels.x), abs(starOffsetPixels.y)));
                float starExists = step(1.0 - density, starRandom);
                float starVariation = lerp(0.55, 1.0, Hash12(seededCell + float2(29.43, 11.97)));

                return starShape * starExists * starVariation * brightness;
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
                float2 pixelsPerWorldUnit = 0.5 * float2(
                    _ScreenParams.x * abs(UNITY_MATRIX_P._m00),
                    _ScreenParams.y * abs(UNITY_MATRIX_P._m11));
                float2 cameraPixelOffsetBase = _WorldSpaceCameraPos.xy * pixelsPerWorldUnit;
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

                if (_UseNebula >= 0.5h)
                {
                    float2 nebulaPixels = input.positionCS.xy
                        + cameraPixelOffsetBase * (float)_NebulaParallax;
                    float2 nebulaCoordinates = nebulaPixels / _ScreenParams.y * (float)_NebulaScale;
                    nebulaCoordinates += (float)_NebulaSeed * float2(7.13, 19.71);
                    nebulaCoordinates.x += nebulaCoordinates.y * 0.35;
                    nebulaCoordinates.y *= 0.7;

                    float broadNebula = SmoothNoise2D(nebulaCoordinates);
                    float detailNebula = SmoothNoise2D(nebulaCoordinates * 2.03 + float2(11.7, 5.3));
                    float nebulaNoise = broadNebula * 0.72 + detailNebula * 0.28;
                    float nebulaMask = smoothstep(
                        (float)_NebulaCoverage - 0.22,
                        (float)_NebulaCoverage + 0.22,
                        nebulaNoise);
                    nebulaMask *= nebulaMask;

                    backgroundColor += _NebulaColor.rgb * nebulaMask * (float)_NebulaStrength;
                }

                if (_UseStars >= 0.5h)
                {
                    float parallaxEnabled = step(0.5, (float)_UseStarParallax);
                    float2 cameraPixelOffset = cameraPixelOffsetBase
                        * (float)_StarParallaxStrength
                        * parallaxEnabled;

                    float farStars = SampleStarLayer(
                        input.positionCS.xy + cameraPixelOffset * (float)_FarStarParallax,
                        (float)_FarStarCellSize,
                        (float)_FarStarDensity,
                        (float)_FarStarSize,
                        (float)_FarStarBrightness,
                        (float)_FarStarSeed);
                    float middleStars = SampleStarLayer(
                        input.positionCS.xy + cameraPixelOffset * (float)_MidStarParallax,
                        (float)_StarCellSize,
                        (float)_StarDensity,
                        (float)_StarSize,
                        (float)_StarBrightness,
                        (float)_StarSeed);
                    float nearStars = SampleStarLayer(
                        input.positionCS.xy + cameraPixelOffset * (float)_NearStarParallax,
                        (float)_NearStarCellSize,
                        (float)_NearStarDensity,
                        (float)_NearStarSize,
                        (float)_NearStarBrightness,
                        (float)_NearStarSeed);

                    backgroundColor += _StarColor.rgb * (farStars + middleStars + nearStars);
                }

                return half4(max(backgroundColor, 0.0h), 1.0h);
            }
            ENDHLSL
        }
    }
}
