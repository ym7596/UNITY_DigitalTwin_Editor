Shader "Custom/HighlightedGrid"
{
    Properties
    {
        [IntRange] _BaseFactor ("Grid Size", Range(2, 25)) = 1
        [IntRange] _DivideFactor ("Grid Divisions", Range(2, 25)) = 10
        _DistanceRatio ("Distance Ratio", Range(0.1, 1)) = 1
        
        _GridLineWidth ("Grid Line Width", Range(0,1.0)) = 0.01
        _GridLineColor ("Grid Line Color", Color) = (1,1,1,1)
        _HighlightColor ("Grid Division Highlight Line Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (0,0,0,1)
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }
        
        Pass
        {
            Cull Back
            ZWrite Off
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            half _BaseFactor;
            half _DivideFactor;
            half _DistanceRatio;
            half _GridLineWidth;
            half4 _GridLineColor, _HighlightColor, _BaseColor;
            
            Varyings vert (Attributes IN)
            {
                Varyings o;
                o.pos = TransformObjectToHClip (IN.vertex.xyz);
                
                float3 worldPos = mul(unity_ObjectToWorld, float4(IN.vertex.xyz, 1.0)).xyz;
                o.uv.yx = worldPos.xz;
                
                return o;
            }

            float GetGridLinePoint(float2 uvDeriv, float2 uv, float gridSize, half gridWidth)
            {
                float divRcp = 1.0 / max(1.0, round(gridSize));
                
                float2 lineUVDeriv = uvDeriv * divRcp;
                float lineWidth = gridWidth * divRcp;
                
                float2 drawWidth = clamp(lineWidth, lineUVDeriv, 0.5);
                float2 lineAA = lineUVDeriv * 1.5;
                float2 gridUV = 1.0 - abs(frac(uv * divRcp) * 2.0 - 1.0);
                
                float2 gridPoint = smoothstep(drawWidth + lineAA, drawWidth - lineAA, gridUV);
                gridPoint *= saturate(lineWidth / drawWidth);
                gridPoint = lerp(gridPoint, lineWidth, saturate(lineUVDeriv * 2.0 - 1.0));
                
                return lerp(gridPoint.x, 1.0, gridPoint.y);
            }
            
            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv.xy;
                float4 uvDDXY = float4(ddx(uv), ddy(uv));
                float2 uvDeriv = float2(length(uvDDXY.xz), length(uvDDXY.yw));
                
                half4 gridLineColor = _GridLineColor;
                half4 highlightLineColor = _HighlightColor;
                const half4 baseColor = _BaseColor;
                
                const bool drawHighlight = _BaseFactor <= 1 && _DistanceRatio < 0.5f ? false : true;
                const float sharpness = drawHighlight ? 1 - _DistanceRatio : 1;
                
                half gridWidth = _GridLineWidth + _BaseFactor == 1 ? 0 : (_GridLineWidth * _BaseFactor * _DistanceRatio);
                
                float gridLinePoint = GetGridLinePoint(uvDeriv, uv, _BaseFactor, gridWidth);
                gridLineColor.a = min(gridLineColor.a, sharpness);

                half drawValue = gridLinePoint * gridLineColor.a;
                half4 outColor = lerp(baseColor, gridLineColor, drawValue);
                
                if(drawHighlight)
                {
                    gridWidth = max(gridWidth, gridWidth * 2 * _DistanceRatio);
                    gridLinePoint = GetGridLinePoint(uvDeriv, uv, _DivideFactor, gridWidth);
                
                    drawValue = gridLinePoint * highlightLineColor.a;
                    outColor = lerp(outColor, highlightLineColor, drawValue);
                }
                
                return outColor;
            }
            ENDHLSL
        }
    }
}
