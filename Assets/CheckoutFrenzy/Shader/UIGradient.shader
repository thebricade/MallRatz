Shader "Unlit/UI Gradient"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorTopLeft ("Top Left Color", Color) = (1,1,1,1)
        _ColorTopRight ("Top Right Color", Color) = (1,1,1,1)
        _ColorBottomLeft ("Bottom Left Color", Color) = (1,1,1,1)
        _ColorBottomRight ("Bottom Right Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // Enable transparency

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _ColorTopLeft;
            fixed4 _ColorTopRight;
            fixed4 _ColorBottomLeft;
            fixed4 _ColorBottomRight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Get the UV coordinates (ranging from 0 to 1)
                float2 uv = i.uv;

                // Bilinear interpolation for the colors
                fixed4 top = lerp(_ColorTopLeft, _ColorTopRight, uv.x);
                fixed4 bottom = lerp(_ColorBottomLeft, _ColorBottomRight, uv.x);
                fixed4 finalColor = lerp(bottom, top, uv.y);

                // Sample the texture and multiply with the gradient color
                fixed4 texColor = tex2D(_MainTex, uv);
                return texColor * finalColor;
            }
            ENDCG
        }
    }
}