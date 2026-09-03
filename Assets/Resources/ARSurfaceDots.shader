Shader "SmartARMeasure/ARSurfaceDots"
{
    Properties
    {
        _PlaneColor ("Ash Plane Color", Color) = (0.75, 0.78, 0.82, 0.12)
        _DotColor ("Dot Color", Color) = (1.0, 1.0, 1.0, 0.90)
        _DotSpacing ("Dot Spacing (Meters)", Float) = 0.075
        _DotRadius ("Dot Radius (Meters)", Float) = 0.0055
        _AlphaMultiplier ("Master Alpha", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+100" 
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 planePos : TEXCOORD0;
            };

            fixed4 _PlaneColor;
            fixed4 _DotColor;
            float _DotSpacing;
            float _DotRadius;
            float _AlphaMultiplier;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // In ARFoundation, ARPlane local X and Z define the 2D plane surface in meters
                o.planePos = v.vertex.xz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float spacing = max(_DotSpacing, 0.01);
                
                // Regular grid coordinates in physical meters
                float2 grid = frac(i.planePos / spacing) - 0.5;
                float dist = length(grid) * spacing;

                // Crisp anti-aliased circular dot with smooth edge
                float radius = max(_DotRadius, 0.001);
                float f = 0.0015;
                float dotMask = 1.0 - smoothstep(radius - f, radius + f, dist);

                // Blend subtle ash base plane with white dots
                fixed4 col;
                col.rgb = lerp(_PlaneColor.rgb, _DotColor.rgb, dotMask);
                float alpha = lerp(_PlaneColor.a, _DotColor.a, dotMask) * _AlphaMultiplier;
                col.a = saturate(alpha);

                return col;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent"
}
