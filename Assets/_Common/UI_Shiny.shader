Shader "UI/Shiny"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Shine)]
        [HDR] _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineIntensity ("Intensity", Range(0, 5)) = 1.5
        _ShineWidth ("Width", Range(0.01, 1)) = 0.18
        _ShineSoftness ("Softness", Range(0.1, 8)) = 2.5
        _ShineAngle ("Angle", Range(-180, 180)) = 25
        _ShineSpeed ("Speed", Range(0, 5)) = 1.2
        _ShinePause ("Pause Between Loops", Range(0, 5)) = 0.8
        _ShineProgress ("Manual Progress (-1 = Auto)", Range(-1, 1)) = -1

        // UI stencil / mask
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float4 _ShineColor;
            float _ShineIntensity;
            float _ShineWidth;
            float _ShineSoftness;
            float _ShineAngle;
            float _ShineSpeed;
            float _ShinePause;
            float _ShineProgress;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float ComputeShine(float2 uv)
            {
                float rad = radians(_ShineAngle);
                float2 dir = float2(cos(rad), sin(rad));
                float proj = dot(uv - 0.5, dir);

                float progress;
                if (_ShineProgress >= 0.0)
                {
                    progress = _ShineProgress;
                }
                else
                {
                    float loopDuration = max(_ShineSpeed, 0.0001);
                    float pause = max(_ShinePause, 0.0);
                    float cycle = 1.0 + pause;
                    float t = frac(_Time.y * loopDuration / cycle) * cycle;
                    progress = saturate(t); // 0..1 during sweep, then pause
                    if (t > 1.0)
                        return 0.0;
                }

                // Sweep from -0.75 .. 0.75 so the band fully crosses the element
                float center = lerp(-0.75, 0.75, progress);
                float d = abs(proj - center);
                float shine = saturate(1.0 - d / max(_ShineWidth, 0.0001));
                return pow(shine, max(_ShineSoftness, 0.01));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                float shine = ComputeShine(i.uv);
                float3 shineRgb = _ShineColor.rgb * shine * _ShineIntensity * color.a;
                color.rgb += shineRgb;

                return color;
            }
            ENDCG
        }
    }
}
