Shader "UI/Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Main Texture", 2D) = "white" {}
        _ShineColor ("Shine Color", Color) = (1,1,1,0.5)
        _ShineWidth ("Shine Width", Range(0.01,0.3)) = 0.1
        _Speed ("Speed", Range(0.1,5)) = 1
        _Delay ("Delay", Range(0,5)) = 2
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _ShineColor;
            float _ShineWidth;
            float _Speed;
            float _Delay;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                
                // 시간 기반 자동 애니메이션
                float cycleTime = 1.0 / _Speed + _Delay;
                float t = fmod(_Time.y, cycleTime);
                float animTime = t * _Speed;
                
                // -0.5 ~ 1.5 범위로 progress 계산
                float progress = lerp(-0.5, 1.5, saturate(animTime));
                
                // 수직 라인 마스크 (왼쪽에서 오른쪽으로)
                float shineMask = 1.0 - saturate(abs(i.uv.x - progress) / _ShineWidth);
                shineMask = smoothstep(0, 1, shineMask);
                
                // 반짝이 적용 (메인 텍스처 알파로 마스킹)
                float shine = shineMask * color.a * _ShineColor.a;
                color.rgb += _ShineColor.rgb * shine;
                
                color.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                
                return color;
            }
            ENDCG
        }
    }
}
