Shader "Turn360To2D/Six Face Equirectangular Blit"
{
    Properties { _MainTex("Equirectangular Source", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float3 _ScreenCentre;
            float3 _ScreenRight;
            float3 _ScreenUp;
            float3 _ViewingPoint;
            static const float kPi = 3.14159265359;

            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Graphics.Blit writes with RenderTexture V orientation.  Flip V here so the
                // displayed RenderTexture maps to exactly the same world-space point as
                // WorldSpace360Screen (the direct-to-wall reference shader).
                float physicalV = 1.0 - input.uv.y;
                float3 screenPoint = _ScreenCentre
                    + (input.uv.x - 0.5) * _ScreenRight
                    + (physicalV - 0.5) * _ScreenUp;
                float3 direction = normalize(screenPoint - _ViewingPoint);
                float longitude = atan2(direction.x, direction.z);
                float latitude = asin(clamp(direction.y, -1.0, 1.0));
                float2 panoramaUv = float2(longitude / (2.0 * kPi) + 0.5, 0.5 + latitude / kPi);
                return tex2D(_MainTex, panoramaUv);
            }
            ENDCG
        }
    }
}
