Shader "PaperUI/Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)

        // Scissor parameters 
        //_ScissorMatrix ("Scissor Matrix", Matrix) = (1,0,0,0)
        _ScissorExtents ("Scissor Extents", Vector) = (1,1,0,0)

        // Brush parameters
        //_BrushMatrix ("Brush Matrix (use SetMatrix)", Vector) = (1,0,0,0)
        _BrushType ("Brush Type", Float) = 0
        _BrushColor1 ("Brush Color 1", Color) = (1,1,1,1)
        _BrushColor2 ("Brush Color 2", Color) = (1,1,1,1)
        _BrushParams ("Brush Params", Vector) = (0,0,0,0)
        _BrushParams2 ("Brush Params2", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            // Uniform variables (properties)
            sampler2D _MainTex;
            float4x4 _ScissorMatrix;
            float2 _ScissorExtents;
            float4x4 _BrushMatrix;
            int _BrushType;
            float4 _BrushColor1;
            float4 _BrushColor2;
            float4 _BrushParams;
            float4 _BrushParams2;
            
            // Input structure for vertex shader
            struct appdata
            {
                float4 vertex : POSITION;     // vertexPosition in GLSL
                float2 uv : TEXCOORD0;        // vertexTexCoord in GLSL
                float4 color : COLOR;         // vertexColor in GLSL
            };

            // Output structure from vertex to fragment shader
            struct v2f
            {
                float4 position : SV_POSITION;   // gl_Position in GLSL
                float2 uv : TEXCOORD0;           // fragTexCoord in GLSL
                float4 color : COLOR;            // fragColor in GLSL
                float2 pos : TEXCOORD1;          // fragPos in GLSL
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.color = v.color;
                o.pos = v.vertex.xy;
                o.position = UnityObjectToClipPos(v.vertex);  // Equivalent to mvp * vec4(vertexPosition, 1.0)
                return o;
            }
            
            float calculateBrushFactor(float2 fragPos) {
                // No brush
                if (_BrushType == 0) return 0.0;
                
                float2 transformedPoint = mul(_BrushMatrix, float4(fragPos, 0.0, 1.0)).xy;

                // Linear brush - projects position onto the line between start and end
                if (_BrushType == 1) {
                    float2 startPoint = _BrushParams.xy;
                    float2 endPoint = _BrushParams.zw;
                    float2 lineVec = endPoint - startPoint;
                    float lineLength = length(lineVec);
                    
                    if (lineLength < 0.001) return 0.0;
                    
                    float2 posToStart = transformedPoint - startPoint;
                    float projection = dot(posToStart, lineVec) / (lineLength * lineLength);
                    return clamp(projection, 0.0, 1.0);
                }
                
                // Radial brush - based on distance from center
                if (_BrushType == 2) {
                    float2 center = _BrushParams.xy;
                    float innerRadius = _BrushParams.z;
                    float outerRadius = _BrushParams.w;
                    
                    if (outerRadius < 0.001) return 0.0;
                    
                    float distance = smoothstep(innerRadius, outerRadius, length(transformedPoint - center));
                    return clamp(distance, 0.0, 1.0);
                }
                
                // Box brush - like radial but uses max distance in x or y direction
                if (_BrushType == 3) {
                    float2 center = _BrushParams.xy;
                    float2 halfSize = _BrushParams.zw;
                    float radius = _BrushParams2.x;
                    float feather = _BrushParams2.y;
                    
                    if (halfSize.x < 0.001 || halfSize.y < 0.001) return 0.0;
                    
                    // Calculate distance from center (normalized by half-size)
                    float2 q = abs(transformedPoint - center) - (halfSize - float2(radius, radius));
                    
                    // Distance field calculation for rounded rectangle
                    float dist = min(max(q.x, q.y), 0.0) + length(max(q, float2(0.0, 0.0))) - radius;
                    
                    return clamp((dist + feather * 0.5) / feather, 0.0, 1.0);
                }
                
                return 0.0;
            }
            
            // Determines whether a point is within the scissor region and returns the appropriate mask value
            // p: The point to test against the scissor region
            // Returns: 1.0 for points fully inside, 0.0 for points fully outside, and a gradient for edge transition
            float scissorMask(float2 p) {
                // Early exit if scissoring is disabled (when any scissor dimension is negative)
                if(_ScissorExtents.x < 0.0 || _ScissorExtents.y < 0.0) return 1.0;
                
                // Transform point to scissor space
                float2 transformedPoint = mul(_ScissorMatrix, float4(p, 0.0, 1.0)).xy;
                
                // Calculate signed distance from scissor edges (negative inside, positive outside)
                float2 distanceFromEdges = abs(transformedPoint) - _ScissorExtents;
                
                // Apply offset for smooth edge transition (0.5 creates half-pixel anti-aliased edges)
                float2 smoothEdges = float2(0.5, 0.5) - distanceFromEdges;
                
                // Clamp each component and multiply to get final mask value
                // Result is 1.0 inside, 0.0 outside, with smooth transition at edges
                return clamp(smoothEdges.x, 0.0, 1.0) * clamp(smoothEdges.y, 0.0, 1.0);
            }
            
            // pixel shader with input from vertex shader
            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate edge alpha for anti-aliasing (equivalent to fwidth and smoothstep in GLSL)
                float2 pixelSize = fwidth(i.uv);
                float2 edgeDistance = min(i.uv, 1.0 - i.uv);
                float edgeAlpha = smoothstep(0.0, pixelSize.x, edgeDistance.x) * smoothstep(0.0, pixelSize.y, edgeDistance.y);
                edgeAlpha = clamp(edgeAlpha, 0.0, 1.0);
                
                float mask = scissorMask(i.pos);
                float4 color = i.color;

                // Apply brush if active
                if (_BrushType > 0) {
                    float factor = calculateBrushFactor(i.pos);
                    color = lerp(_BrushColor1, _BrushColor2, factor);
                }
                
                float4 textureColor = tex2D(_MainTex, i.uv);
                color *= textureColor;
                color *= edgeAlpha * mask;
                return color;
            }
            ENDHLSL
        }
    }
}