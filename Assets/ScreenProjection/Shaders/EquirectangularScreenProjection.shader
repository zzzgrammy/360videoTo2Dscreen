Shader "Turn360To2D/EquirectangularScreenProjection"
{
    Properties { _MainTex("Equirectangular 360 Video", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float3 _ScreenCentre;
            float3 _ScreenRight;
            float3 _ScreenUp;
            float3 _IdealViewingPoint;
            float _SourceLatitudeIsFlipped;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(output.uv * 2.0 - 1.0, 0.0, 1.0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Physical point on the flat output screen. Its ray starts at the calibrated human-eye point.
                float3 screenPoint = _ScreenCentre
                    + (input.uv.x - 0.5) * _ScreenRight
                    + (input.uv.y - 0.5) * _ScreenUp;
                float3 direction = normalize(screenPoint - _IdealViewingPoint);

                // Unity coordinates: forward +Z, right +X, up +Y.  The texture is equirectangular.
                float longitude = atan2(direction.x, direction.z);
                float latitude = asin(clamp(direction.y, -1.0, 1.0));
                float sourceV = lerp(0.5 - latitude / PI, 0.5 + latitude / PI, _SourceLatitudeIsFlipped);
                float2 sourceUv = float2(longitude * (1.0 / (2.0 * PI)) + 0.5, sourceV);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sourceUv);
            }
            ENDHLSL
        }
    }
}
