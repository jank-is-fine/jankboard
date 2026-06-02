#version 330 core
layout (location = 0) in vec2 vQuadPos; 
layout (location = 1) in vec2 vInstancePos;
layout (location = 2) in vec2 vScale;
layout (location = 3) in vec4 vUv;
layout (location = 4) in vec4 vColor;
layout (location = 5) in float vPxRange;

uniform mat4 uProjection;
uniform mat4 uView;

out vec2 fUv;
out vec4 fColor;
out float fPxRange;

void main()
{
    vec2 finalPos = vInstancePos + vQuadPos * vScale;
    
    vec2 uv = mix(vUv.xy, vUv.zw, vQuadPos);
    
    fUv = uv;
    fColor = vColor;
    fPxRange = vPxRange;
    
    gl_Position = uProjection * uView * vec4(finalPos, 0.0, 1.0);
}