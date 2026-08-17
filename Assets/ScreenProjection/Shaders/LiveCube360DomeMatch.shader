Shader "Turn360To2D/Live Cube 360 Dome Match"
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
            // Render both the inner and outer side of every independently
            // selectable wall plane.
            Cull Off
            ZWrite On

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Exposure;
            static const float kPi = 3.14159265359;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // This is deliberately the same ray definition as
                // Inside 360 Dome: current camera -> visible surface point.
                // It therefore follows the camera in real time without any
                // C# updates or a pre-rendered six-face texture.
                float3 direction = normalize(input.positionWS - _WorldSpaceCameraPos);
                float longitude = atan2(direction.x, direction.z);
                float latitude = asin(clamp(direction.y, -1.0, 1.0));
                float2 uv = float2(longitude * (1.0 / (2.0 * kPi)) + 0.5, 0.5 + latitude / kPi);
                return tex2D(_MainTex, uv) * _Exposure;
            }
            ENDCG
        }
    }
}
