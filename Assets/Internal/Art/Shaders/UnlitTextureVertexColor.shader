Shader "Custom/UnlitTextureVertexColor"
{
  Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidthUV ("Outline Width (UV space)", Range(0.0001, 0.1)) = 0.02
        _OutlineSmooth  ("Outline Smooth", Range(0.0, 0.05)) = 0.005
        _TopNormalDotMin ("Top Normal Threshold", Range(0.0, 1.0)) = 0.9
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1; // outline용 uv2
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv0         : TEXCOORD0;
                float2 uv1         : TEXCOORD1;
                float4 color       : COLOR;
                float3 normalWS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidthUV;
                float  _OutlineSmooth;
                float  _TopNormalDotMin;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv0 = TRANSFORM_TEX(IN.uv0, _BaseMap);
                OUT.uv1 = IN.uv1; 
                OUT.color = IN.color;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv0) * IN.color * _BaseColor;
                
                float isTopByNormal = step(_TopNormalDotMin, dot(normalize(IN.normalWS), float3(0,1,0)));
                float isUv1Valid = step(0.0, min(IN.uv1.x, IN.uv1.y)); 
                
                float2 d = min(IN.uv1, 1.0 - IN.uv1); 
                float edgeDist = min(d.x, d.y);      
                float edge = smoothstep(_OutlineWidthUV + _OutlineSmooth, _OutlineWidthUV, edgeDist);
                float outlineMask = edge * isTopByNormal * isUv1Valid;
                half4 col = lerp(albedo, _OutlineColor, outlineMask);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
