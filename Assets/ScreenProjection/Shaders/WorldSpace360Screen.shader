Shader "Turn360To2D/World Space 360 Screen"
{
    Properties { _MainTex("Equirectangular 360 Image", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float3 _ViewingPoint;
            static const float kPi = 3.14159265359;

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.positionWS - _ViewingPoint);
                float longitude = atan2(direction.x, direction.z);
                float latitude = asin(clamp(direction.y, -1.0, 1.0));
                // Imported Unity texture coordinates are bottom-origin; 006.png is standard equirectangular.
                float2 panoramaUv = float2(longitude * (1.0 / (2.0 * kPi)) + 0.5, 0.5 + latitude / kPi);
                return tex2D(_MainTex, panoramaUv);
            }
            ENDHLSL
        }
    }
}
