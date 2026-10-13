Shader "Custom/Brush"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OriginalTex ("Original Texture", 2D) = "white" {}

        _BrushPos ("Brush Position", Vector) = (0,0,0,0)
        _BrushColor ("Brush Color", Color) = (1,0,0,1)
        _BrushSize ("Brush Size", Float) = 0.05

        _BrushEnabled ("Brush Enabled", Float) = 0
        _EraserEnabled ("Eraser Enabled", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_OriginalTex);
            SAMPLER(sampler_OriginalTex);

            float4 _BrushPos;
            float4 _BrushColor;

            float _BrushSize;
            float _BrushEnabled;
            float _EraserEnabled;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Current painted texture
                half4 col =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        IN.uv
                    );

                float d =
                    distance(
                        IN.uv,
                        _BrushPos.xy
                    );

                // Normal painting
                if (_BrushEnabled > 0.5 && d < _BrushSize)
                {
                    col = _BrushColor;
                }

                // Eraser
                if (_EraserEnabled > 0.5 && d < _BrushSize)
                {
                    col =
                        SAMPLE_TEXTURE2D(
                            _OriginalTex,
                            sampler_OriginalTex,
                            IN.uv
                        );
                }

                return col;
            }

            ENDHLSL
        }
    }
}