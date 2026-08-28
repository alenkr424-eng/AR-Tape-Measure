Shader "SmartARMeasure/ARSurfaceDots"
{
    Properties
    {
        _PlaneColor ("Ash Plane Color", Color) = (0.22, 0.25, 0.28, 0.22)
        _DotColor ("Dot Color", Color) = (0.85, 0.88, 0.92, 0.70)
        _DotSpacing ("Dot Spacing (Meters)", Float) = 0.08
        _DotRadius ("Dot Radius", Float) = 0.09
        _DotFeather ("Dot Feather", Float) = 0.025
        _AlphaMultiplier ("Master Alpha", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"
        }
        LOD 100

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ARSurfaceDotsForward"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            fixed4 _PlaneColor;
            fixed4 _DotColor;
            float _DotSpacing;
            float _DotRadius;
            float _DotFeather;
            float _AlphaMultiplier;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_AlphaMultiplier <= 0.001)
                {
                    discard;
                }

                // Calculate planar 2D grid coordinates based on world space positions
                // This ensures perfectly uniform dot spacing across all detected planes
                float spacing = max(_DotSpacing, 0.01);
                float2 planarCoord = float2(i.worldPos.x, i.worldPos.z) / spacing;

                // Center grid cells from [-0.5, 0.5]
                float2 grid = frac(planarCoord) - 0.5;
                float dist = length(grid);

                // Anti-aliased smooth circular dot mask
                float r = max(_DotRadius, 0.01);
                float f = max(_DotFeather, 0.005);
                float dotMask = 1.0 - smoothstep(r - f, r + f, dist);

                // Composite ash background surface and subtle dots
                fixed4 col;
                col.rgb = lerp(_PlaneColor.rgb, _DotColor.rgb, dotMask);
                
                float combinedAlpha = lerp(_PlaneColor.a, _DotColor.a, dotMask);
                col.a = combinedAlpha * _AlphaMultiplier;

                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "ARSurfaceDotsUniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            fixed4 _PlaneColor;
            fixed4 _DotColor;
            float _DotSpacing;
            float _DotRadius;
            float _DotFeather;
            float _AlphaMultiplier;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_AlphaMultiplier <= 0.001)
                {
                    discard;
                }

                float spacing = max(_DotSpacing, 0.01);
                float2 planarCoord = float2(i.worldPos.x, i.worldPos.z) / spacing;

                float2 grid = frac(planarCoord) - 0.5;
                float dist = length(grid);

                float r = max(_DotRadius, 0.01);
                float f = max(_DotFeather, 0.005);
                float dotMask = 1.0 - smoothstep(r - f, r + f, dist);

                fixed4 col;
                col.rgb = lerp(_PlaneColor.rgb, _DotColor.rgb, dotMask);
                
                float combinedAlpha = lerp(_PlaneColor.a, _DotColor.a, dotMask);
                col.a = combinedAlpha * _AlphaMultiplier;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
