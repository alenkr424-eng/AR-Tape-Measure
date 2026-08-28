Shader "SmartARMeasure/MeasurementOverlay"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0.9, 1, 1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 8 // 8 = Always
    }
    SubShader
    {
        Tags 
        { 
            "Queue" = "Geometry+600" 
            "RenderType" = "Opaque" 
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"
        }
        LOD 100

        ZTest [_ZTest]
        ZWrite Off
        Cull Off
        Blend Off

        Pass
        {
            Name "MeasurementOverlayForward"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // If LineRenderer or mesh supplies vertex color, tint with _Color; else use _Color
                fixed4 vertCol = (v.color.r + v.color.g + v.color.b > 0.01) ? v.color : fixed4(1, 1, 1, 1);
                o.color = vertCol * _Color;
                o.color.a = 1.0;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }

        Pass
        {
            Name "MeasurementOverlayAlways"
            Tags { "LightMode" = "Always" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                fixed4 vertCol = (v.color.r + v.color.g + v.color.b > 0.01) ? v.color : fixed4(1, 1, 1, 1);
                o.color = vertCol * _Color;
                o.color.a = 1.0;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
