Shader "Turn360To2D/Inside 360 Dome"
{
    Properties
    {
        _MainTex("Equirectangular 360 Image", 2D) = "white" {}
        _Exposure("Exposure", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            // A normal Unity sphere has outward-facing triangles. Cull them so the inside is visible.
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Exposure;
            static const float kPi = 3.14159265359;

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 directionWS : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.directionWS = normalize(positionWS.xyz - _WorldSpaceCameraPos);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.directionWS);
                float longitude = atan2(direction.x, direction.z);
                float latitude = asin(clamp(direction.y, -1.0, 1.0));
                // Unity sphere's inside-facing orientation requires this V direction for an upright panorama.
                float2 uv = float2(longitude * (1.0 / (2.0 * kPi)) + 0.5, 0.5 + latitude / kPi);
                return tex2D(_MainTex, uv) * _Exposure;
            }
            ENDCG
        }
    }
}
