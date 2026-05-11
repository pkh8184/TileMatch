Shader "UI/FrozenWaterBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Ice Colors)]
        _IceColor    ("Ice Color",          Color) = (0.55, 0.85, 1.00, 1)
        _HighlightColor ("Highlight Color", Color) = (0.85, 0.95, 1.00, 1)

        [Header(Outer Border)]
        _BorderColor ("Border Color",       Color) = (0.10, 0.40, 0.65, 1)
        _BorderWidth ("Border Width",       Range(0, 0.1)) = 0.015
        _BorderSharp ("Border Sharpness",   Range(1, 32)) = 8

        [Header(Cell Variation)]
        _CellVariation ("Cell Shape Variation", Range(0, 1)) = 0.5
        _CellFlowSpeed ("Cell Flow Speed",      Range(0, 2)) = 0.4

        [Header(Frozen Edge)]
        _EdgeWidth   ("Edge Width",       Range(0, 1)) = 0.35
        _EdgeSharp   ("Edge Sharpness",   Range(1, 16)) = 4
        _IcePatternScale ("Ice Pattern Scale", Range(1, 20)) = 8
        _IcePatternStrength ("Ice Pattern Strength", Range(0, 2)) = 1.0

        [Header(Water Motion)]
        _WaveScale   ("Wave Scale",       Range(0.5, 10)) = 3
        _WaveSpeed   ("Wave Speed",       Range(0, 2)) = 0.3
        _WaveStrength("Wave Strength",    Range(0, 0.2)) = 0.05
        _Distortion  ("Distortion Amount", Range(0, 0.1)) = 0.02

        [Header(Freeze Animation)]
        _FreezeAmount ("Freeze Amount (0=Off, 1=Full)", Range(0, 1)) = 1.0
        _FreezeNoiseScale ("Freeze Edge Noise Scale", Range(1, 30)) = 8
        _FreezeNoiseStrength ("Freeze Edge Irregularity", Range(0, 1)) = 0.4

        [Header(Aspect)]
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 0.5625 // 1080/1920

        // UI Mask 기본값
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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;

            fixed4 _IceColor, _HighlightColor, _BorderColor;
            float _BorderWidth, _BorderSharp;
            float _CellVariation, _CellFlowSpeed;
            float _FreezeAmount, _FreezeNoiseScale, _FreezeNoiseStrength;
            float _EdgeWidth, _EdgeSharp;
            float _IcePatternScale, _IcePatternStrength;
            float _WaveScale, _WaveSpeed, _WaveStrength, _Distortion;
            float _AspectRatio;

            // --- 노이즈 헬퍼 ---
            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453) * 2.0 - 1.0;
            }

            float hash12(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Value noise
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash12(i);
                float b = hash12(i + float2(1, 0));
                float c = hash12(i + float2(0, 1));
                float d = hash12(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // FBM
            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * valueNoise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            // Voronoi (얼음 결정용)
            float2 voronoi(float2 p)
            {
                float2 n = floor(p);
                float2 f = frac(p);

                float minDist = 1.0;
                float secondMin = 1.0;

                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 g = float2(i, j);
                        float2 o = hash22(n + g) * 0.5 + 0.5;
                        float2 r = g + o - f;
                        float d = dot(r, r);

                        if (d < minDist)
                        {
                            secondMin = minDist;
                            minDist = d;
                        }
                        else if (d < secondMin)
                        {
                            secondMin = d;
                        }
                    }
                }
                return float2(sqrt(minDist), sqrt(secondMin));
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPos = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.uv = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 종횡비 보정된 UV (셀/노이즈가 정사각형으로 보이도록)
                // _AspectRatio = W/H (예: 1080/1920 = 0.5625)
                // 세로축을 늘려서 정사각형 공간에서 노이즈 샘플링
                float2 uv = IN.uv;
                float2 auv = float2(uv.x, uv.y / max(_AspectRatio, 0.001));

                float time = _Time.y;

                // --- 1. 외곽까지의 거리 (모서리에서 가까울수록 1) ---
                // 가로/세로 가장자리에서 동일한 두께의 액자가 되도록
                float2 centered = abs(uv - 0.5) * 2.0; // 0~1
                float edgeDist = max(centered.x, centered.y);

                // --- 1-1. 얼음 성장(Freeze) 마스크 ---
                // _FreezeAmount = 0 → 외곽에도 얼음 없음
                // _FreezeAmount = 1 → 풀 _EdgeWidth 만큼 얼음
                // 노이즈를 더해 경계가 들쭉날쭉하게 (자연스럽게 얼어오는 느낌)
                float freezeNoise = fbm(auv * _FreezeNoiseScale + time * 0.08);
                // -strength/2 ~ +strength/2 범위로 오프셋
                float freezeOffset = (freezeNoise - 0.5) * _FreezeNoiseStrength;

                // 현재 얼음 영역의 안쪽 경계 (1.0에서 안쪽으로 들어옴)
                // _FreezeAmount=0이면 inner=1.0 (얼음 0), _FreezeAmount=1이면 inner=1.0-_EdgeWidth
                float innerBoundary = 1.0 - _EdgeWidth * _FreezeAmount + freezeOffset * _EdgeWidth;

                // 외곽 마스크: edgeDist > innerBoundary인 영역만 얼음
                float edgeMask = smoothstep(innerBoundary - 0.02, innerBoundary + 0.02, edgeDist);

                // _FreezeAmount가 0에 가까우면 완전히 꺼지도록 (잔여 노이즈 제거)
                edgeMask *= smoothstep(0.0, 0.05, _FreezeAmount);

                edgeMask = pow(edgeMask, 1.0 / max(_EdgeSharp, 0.001));

                // --- 2. 물결 왜곡 ---
                float2 waveUV = auv * _WaveScale;
                float2 distortion;
                distortion.x = fbm(waveUV + time * _WaveSpeed);
                distortion.y = fbm(waveUV + 7.13 + time * _WaveSpeed * 0.85);
                distortion = (distortion - 0.5) * _Distortion;

                float2 distortedUV = auv + distortion;

                // --- 3. 물결 흐름 (얼음 영역에 살짝 입힐 패턴) ---
                float wave1 = fbm(distortedUV * _WaveScale + time * _WaveSpeed * 0.5);
                float wave2 = fbm(distortedUV * _WaveScale * 1.7 - time * _WaveSpeed * 0.3);
                float waterPattern = (wave1 * 0.6 + wave2 * 0.4);

                // --- 4. 얼음 결정 패턴 (Voronoi) ---
                float2 iceUV = auv * _IcePatternScale;
                // 베이스 셀 위치는 거의 고정 (살짝만 흐름)
                iceUV += float2(sin(time * 0.05), cos(time * 0.06)) * 0.1;

                // 셀 모양 워핑 - 시간에 따라 다른 방향으로 흐르는 두 레이어
                // 큰 흐름: 셀 경계가 출렁이는 느낌
                float2 cellWarp;
                cellWarp.x = fbm(iceUV * 0.6 + float2(time * _CellFlowSpeed * 0.3, 0));
                cellWarp.y = fbm(iceUV * 0.6 + float2(13.7, -time * _CellFlowSpeed * 0.25));
                iceUV += (cellWarp - 0.5) * _CellVariation * 2.0;

                // 작은 디테일: 더 빠르게 움직여서 셀 경계가 미세하게 떨림
                float2 microWarp;
                microWarp.x = fbm(iceUV * 2.5 + float2(0, time * _CellFlowSpeed * 0.6));
                microWarp.y = fbm(iceUV * 2.5 + float2(5.3 - time * _CellFlowSpeed * 0.5, 0));
                iceUV += (microWarp - 0.5) * _CellVariation * 0.5;

                float2 vor = voronoi(iceUV);
                // 셀 경계 라인 (얼음 균열)
                float iceLines = smoothstep(0.0, 0.08, vor.y - vor.x);
                iceLines = 1.0 - iceLines; // 라인을 1로

                // 얼음 셀 내부의 미세한 변화
                float iceFill = vor.x;

                // 얼음 색상 합성
                float3 iceCol = lerp(_IceColor.rgb, _HighlightColor.rgb, iceLines * 0.7);
                iceCol = lerp(iceCol * 0.85, iceCol, iceFill);

                // 물결 하이라이트 약간 추가
                iceCol += _HighlightColor.rgb * waterPattern * _WaveStrength;

                // 얼음 라인 강조
                iceCol += _HighlightColor.rgb * iceLines * 0.3;

                // --- 5. 외곽 영역 알파 + 화면 가장자리 보더 ---
                float iceAlpha = saturate(edgeMask * _IcePatternStrength);

                // 화면 끝(edgeDist = 1.0)에 진한 보더 한 겹
                float borderMask = smoothstep(1.0 - _BorderWidth, 1.0, edgeDist);
                borderMask = pow(borderMask, 1.0 / max(_BorderSharp, 0.001));
                // 보더도 얼음과 함께 등장하도록
                borderMask *= smoothstep(0.0, 0.1, _FreezeAmount);

                // 얼음 위에 보더 컬러를 오버레이
                iceCol = lerp(iceCol, _BorderColor.rgb, borderMask);

                // 최종 알파는 얼음 + 보더 영역 합집합
                float alpha = max(iceAlpha, borderMask);

                // --- 6. UI Texture & Mask ---
                fixed4 texColor = tex2D(_MainTex, IN.uv);
                fixed4 col = fixed4(iceCol, alpha) * IN.color;
                col.a *= texColor.a;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
