// Drains the colour out of the picture (ColorDrain.cs): _Saturation 1 = as drawn, 0 = black and white.
// A full-screen image effect for the built-in render pipeline.
Shader "Hidden/Tidecrown/Desaturate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0, 1)) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Saturation;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float grey = dot(c.rgb, float3(0.299, 0.587, 0.114));
                // A cool, slightly dim grey rather than flat, so the grey world still looks like a place.
                float3 drained = float3(grey * 0.96, grey * 0.98, grey * 1.04);
                c.rgb = lerp(drained, c.rgb, _Saturation);
                return c;
            }
            ENDCG
        }
    }
}
