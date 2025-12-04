Shader "Custom/HeatmapShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _Radius ("Radius", Float) = 5.0
        _RadiusWeightK ("Radius Weight K", Range(0,1)) = 0.25
        _MaxIntensity ("Max Intensity", Float) = 1.0

        _Sigma ("Gaussian Sigma (fraction of R)", Range(0.1,1.0)) = 0.35

        _Color0("Color 0", Color) = (0.05, 0.35, 1.00, 1.0)
        _Color1("Color 1", Color) = (0.05, 0.95, 1.00, 1.0)
        _Color2("Color 2", Color) = (0.90, 1.00, 0.30, 1.0)
        _Color3("Color 3", Color) = (0.95, 0.80, 0.10, 1.0)
        _Color4("Color 4", Color) = (1.00, 0.10, 0.05, 1.0)

        _Range0("Range 0", Range(0,1)) = 0.00
        _Range1("Range 1", Range(0,1)) = 0.25
        _Range2("Range 2", Range(0,1)) = 0.50
        _Range3("Range 3", Range(0,1)) = 0.75
        _Range4("Range 4", Range(0,1)) = 1.00

        _Strength ("Strength", Range(0.1, 4.0)) = 1.0
        _IntensityScale ("Intensity Scale", Float) = 1.25
        _IntensityPow ("Intensity Power", Float) = 0.85
        _MinAlpha ("Min Alpha", Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            // WebGL 호환성을 위해 target 2.0으로 변경
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            // WebGL 최적화 옵션
            #pragma only_renderers gles gles3 d3d11 glcore

            #include "UnityCG.cginc"

            // WebGL uniform 제한을 고려하여 배열 크기 축소 (256으로 제한)
            #define MAX_POSITIONS 256

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex   : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // 배열 크기 축소
            uniform float4 _Positions[MAX_POSITIONS];
            uniform int _PositionCount;

            float _Radius;
            float _MaxIntensity;
            float _Sigma;

            float4 _Color0, _Color1, _Color2, _Color3, _Color4;
            float _Range0, _Range1, _Range2, _Range3, _Range4;

            float _Strength;
            float _IntensityScale;
            float _IntensityPow;
            float _RadiusWeightK;
            float _MinAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            float3 GetRampColor(float t)
            {
                t = saturate(t);

                float3 colors[5];
                colors[0] = _Color0.rgb;
                colors[1] = _Color1.rgb;
                colors[2] = _Color2.rgb;
                colors[3] = _Color3.rgb;
                colors[4] = _Color4.rgb;

                float ranges[5];
                ranges[0] = _Range0;
                ranges[1] = _Range1;
                ranges[2] = _Range2;
                ranges[3] = _Range3;
                ranges[4] = _Range4;

                // 구간별 보간 (unroll 제거로 WebGL 호환성 향상)
                if (t <= ranges[1])
                {
                    float f = saturate((t - ranges[0]) / max(0.00001, ranges[1] - ranges[0]));
                    return lerp(colors[0], colors[1], f);
                }
                else if (t <= ranges[2])
                {
                    float f = saturate((t - ranges[1]) / max(0.00001, ranges[2] - ranges[1]));
                    return lerp(colors[1], colors[2], f);
                }
                else if (t <= ranges[3])
                {
                    float f = saturate((t - ranges[2]) / max(0.00001, ranges[3] - ranges[2]));
                    return lerp(colors[2], colors[3], f);
                }
                else
                {
                    float f = saturate((t - ranges[3]) / max(0.00001, ranges[4] - ranges[3]));
                    return lerp(colors[3], colors[4], f);
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv);

                float total = 0.0;
                float R = max(0.00001, _Radius);
                float sigma = max(0.0001, _Sigma);

                // 실제 카운트를 MAX_POSITIONS로 제한
                int count = min(_PositionCount, MAX_POSITIONS);

                // 동적 루프를 고정 루프로 변경 (WebGL 호환성)
                for (int idx = 0; idx < MAX_POSITIONS; idx++)
                {
                    // 조기 종료 조건
                    if (idx >= count) break;

                    float3 pos    = _Positions[idx].xyz;
                    float  weight = _Positions[idx].w;

                    float2 d2   = float2(i.worldPos.x - pos.x, i.worldPos.z - pos.z);
                    float  dist = length(d2);

                    // Weight에 따라 반경 확장
                    float effR  = R * (1.0 + _RadiusWeightK * sqrt(max(0.0, weight)));
                    //float distN = dist / effR;
                    float distN = dist / R;
                    if (distN < 1.0)
                    {
                        /*float sigmaSq = sigma * sigma;
                        float distNSq = distN * distN;
                        float g = exp(-distNSq / (2.0 * sigmaSq));*/
                        float sigmaEff = sigma * (1.0 + _RadiusWeightK * sqrt(max(0.0, weight)));
                        float sigmaEffSq = sigmaEff * sigmaEff;

                        float dNSq = distN * distN;
                        float g = exp(-dNSq / (2.0 * sigmaEffSq));

                        total += weight * g;
                    }
                }

                total *= _Strength;

                // 강도 정규화 및 스케일링
                float t = total / max(_MaxIntensity, 0.00001);
                t = saturate(t * _IntensityScale);
                t = pow(max(t, 0.0), _IntensityPow);

                float3 heatRGB = GetRampColor(t);
                float  alpha   = max(_MinAlpha, t);

                fixed4 finalColor = fixed4(heatRGB, alpha);
                finalColor.rgb = lerp(baseCol.rgb, finalColor.rgb, finalColor.a);

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return finalColor;
            }
            ENDCG
        }
    }

    // WebGL 폴백
    FallBack "Transparent/VertexLit"
}