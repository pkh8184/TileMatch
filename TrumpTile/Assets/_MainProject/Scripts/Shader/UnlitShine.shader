Shader "UI/UnlitShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Target Texture", 2D) = "white" {}
        _ShineTex   ("Shine Texture", 2D)          = "white" {}
        _Color      ("Image Color",   Color)       = (1,1,1,1)
        _ShineColor ("Shine Color",   Color)       = (1,1,1,1)
        _Period     ("Period (sec)",  Float)            = 2.0
        _DutyRatio  ("Duty Ratio",    Range(0.01, 1.0)) = 0.3
        _AngleDeg   ("Angle (deg)",   Float)            = 45.0
        _Alpha      ("Image Alpha",   Range(0, 1))      = 1.0
        _ShineAlpha ("Shine Alpha",   Range(0, 1))      = 1.0

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
            "Queue"            = "Transparent"
            "IgnoreProjector"  = "True"
            "RenderType"       = "Transparent"
            "PreviewType"      = "Plane"
            "CanUseSpriteAtlas"= "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _ShineTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            fixed4    _ShineColor;
            float     _Period;
            float     _DutyRatio;
            float     _AngleDeg;
            float     _Alpha;
            float     _ShineAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float phase   = frac(_Time.y / max(_Period, 0.0001));
                float activeT = phase / max(_DutyRatio, 0.0001);

                float  a   = _AngleDeg * (UNITY_PI / 180.0);
                float2 dir = float2(cos(a), sin(a));

                float2 lightCenter = (2.0 * activeT - 1.0) * dir;
                float2 shineUV     = i.uv - lightCenter;

                fixed4 baseCol  = tex2D(_MainTex,  i.uv)   * i.color * _Color;
                fixed4 shineCol = tex2D(_ShineTex, shineUV) * _ShineColor;

                baseCol.a *= _Alpha;

                float shineMask = step(activeT, 1.0);

                fixed3 rgb = baseCol.rgb
                           + shineCol.rgb * shineCol.a * _ShineAlpha * baseCol.a * shineMask;

                return fixed4(rgb, baseCol.a);
            }
            ENDCG
        }
    }
}