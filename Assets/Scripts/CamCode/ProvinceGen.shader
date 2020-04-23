Shader "Custom/ProvinceGen"
{
    Properties
    {
        _MainTex("Base (RGBA)", 2D) = "white" {}
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
        
        sampler2D _MainTex;
        float4 _MainTex_ST, _MainTex_TexelSize;
        StructuredBuffer<float4> floatBuffer;
        int countryBorderToggle; // 1 is provinces, 0 is only countries.
        
        V2F Vert(VertexData v) {
            V2F i;
            i.position = UnityObjectToClipPos(v.position);
            i.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
            return i;
        }
        
        // LITERALLY MAGIC: DO NOT TOUCH!
        float4 Frag(V2F i) : SV_Target {
            // Looking up pixel in ID color map
            // TURN OFF HDR AND MSAA ON CAMERA TO SAMPLE PROPER COLORS!!!
            float4 p = tex2D(_MainTex, i.uv);
            /*
            Determining province color.
            R and G reserved for province id. Max 65536 provinces.
            B and A are state id. Same max as above.
            */
            int index = ceil(p.r * 255 + p.g * 255 * 256);
            int state = ceil(p.b * 255 + p.a * 255 * 256);
            
            // Sampling cardinal directions for border
            float4 up       = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y));
            float4 down     = tex2D(_MainTex, i.uv - float2(0, _MainTex_TexelSize.y));
            float4 left     = tex2D(_MainTex, i.uv - float2(_MainTex_TexelSize.x, 0));
            float4 right    = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0));
        
            int iU = ceil(up.r * 255 + up.g * 255 * 256);
            int iD = ceil(down.r * 255 + down.g * 255 * 256);
            int iL = ceil(left.r * 255 + left.g * 255 * 256);
            int iR = ceil(right.r * 255 + right.g * 255 * 256);
        
            int sU = ceil(up.b * 255 + up.a * 255 * 256);
            int sD = ceil(down.b * 255 + down.a * 255 * 256);
            int sL = ceil(left.b * 255 + left.a * 255 * 256);
            int sR = ceil(right.b * 255 + right.a * 255 * 256);
            
            float4 curProvColor = floatBuffer[index];
                
            float stateSum = saturate(abs(sU - state) + abs(sD - state) + abs(sL - state) + abs(sR - state));
            
            float4 borderColor = float4(curProvColor.rgb * (1 - stateSum) * 0.75 * countryBorderToggle, 1);
            
            float sum = countryBorderToggle * (abs(iU - index) + abs(iD - index) + abs(iL - index) + abs(iR - index)) + 
                (1 - countryBorderToggle) * dot(abs(floatBuffer[iU] - curProvColor) + abs(floatBuffer[iD] - curProvColor) +
                abs(floatBuffer[iL] - curProvColor) + abs(floatBuffer[iR] - curProvColor), float4(1,1,1,0)); 
            
            return lerp(curProvColor, borderColor, step(0.01, sum));
        }
            
        ENDCG
        }
    }
    Fallback Off
}