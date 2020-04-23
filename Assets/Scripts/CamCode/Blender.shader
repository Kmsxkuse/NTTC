Shader "Custom/Blender"
{
    Properties
    {
        _MainTex("Base (RGBA)", 2D) = "white" {}
        _SecondTex("Base (RGBA)", 2D) = "white" {}
    }
    
    SubShader
    {
        Pass
        {
        CGPROGRAM
            
        #pragma vertex Vert
        #pragma fragment Frag
        
        //https://catlikecoding.com/unity/tutorials/rendering/part-2/
        #include "UnityCG.cginc"
        
        struct V2F {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
        };
        
        struct VertexData {
            float4 position : POSITION;
            float2 uv : TEXCOORD0;
        };
        
        sampler2D _MainTex, _SecondTex;
        float4 _MainTex_ST, _SkipColor;
        float _SkipDirection,  // 0 activates blend if main = skip. 1 blends if main =/= skip.
            _BlendStrength;
        
        V2F Vert(VertexData v) {
            V2F i;
            i.position = UnityObjectToClipPos(v.position);
            i.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
            return i;
        }
        
        float4 Frag(V2F i) : SV_Target {
            float4 main = tex2D(_MainTex, i.uv);
            
            // Color skipping
            float4 difference = main - _SkipColor;
            float sum = abs(difference.r) + abs(difference.g) + abs(difference.b);
            
            float skipSwitch = abs(step(sum, 0.01) - step(_SkipDirection, 0.01));
            float4 secondary = lerp(tex2D(_SecondTex, i.uv), main, skipSwitch);
            
            return lerp(main, secondary, _BlendStrength);
        }
            
        ENDCG
        }
    }
    Fallback Off
}