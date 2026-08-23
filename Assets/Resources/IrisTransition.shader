Shader "Astrodiver/UI/IrisTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _OutsideColor ("Outside Color", Color) = (0, 0, 0, 1)
        _Radius ("Radius", Float) = 1
        _EdgeSoftness ("Edge Softness", Range(0.0001, 0.1)) = 0.008

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "IrisTransition"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _OutsideColor;
            float _Radius;
            float _EdgeSoftness;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.texcoord = input.texcoord;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 position = input.texcoord - 0.5f;
                position.x *= _ScreenParams.x / _ScreenParams.y;

                float distanceToCenter = length(position);
                float outsideMask = smoothstep(
                    _Radius - _EdgeSoftness,
                    _Radius,
                    distanceToCenter);

                fixed4 color = _OutsideColor;
                color.a *= outsideMask * input.color.a;
                return color;
            }
            ENDCG
        }
    }
}
