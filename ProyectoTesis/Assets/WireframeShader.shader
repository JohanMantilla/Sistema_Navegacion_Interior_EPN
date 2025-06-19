Shader "Custom/Wireframe"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,1)
        _WireThickness ("Wire Thickness", Range(0, 10)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            
            #include "UnityCG.cginc"
            
            fixed4 _Color;
            float _WireThickness;
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2g
            {
                float4 pos : SV_POSITION;
            };
            
            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };
            
            v2g vert (appdata v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                
                o.pos = input[0].pos;
                o.barycentric = float3(1, 0, 0);
                triStream.Append(o);
                
                o.pos = input[1].pos;
                o.barycentric = float3(0, 1, 0);
                triStream.Append(o);
                
                o.pos = input[2].pos;
                o.barycentric = float3(0, 0, 1);
                triStream.Append(o);
            }
            
            fixed4 frag (g2f i) : SV_Target
            {
                float3 barys = i.barycentric;
                float deltas = fwidth(barys);
                float3 smoothing = deltas * _WireThickness;
                float3 thickness = smoothstep(float3(0,0,0), smoothing, barys);
                float minThickness = min(thickness.x, min(thickness.y, thickness.z));
                
                return fixed4(_Color.rgb, 1 - minThickness);
            }
            ENDCG
        }
    }
}