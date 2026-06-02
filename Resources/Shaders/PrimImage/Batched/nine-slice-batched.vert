#version 330 core

layout (location = 0) in vec2 vQuadPos;
layout (location = 1) in vec2 vQuadUV;
layout (location = 2) in vec2 vInstancePos;
layout (location = 3) in vec2 vInstanceScale;
layout (location = 4) in vec4 vInstanceColor;
layout (location = 5) in vec2 vInstanceBorder;

uniform mat4 uView;
uniform mat4 uProjection;

out vec2 fUv;
out vec4 fColor;
out vec2 fDimensions;
out vec2 fBorder;

void main()
{
    vec2 worldPos = vInstancePos + (vQuadPos - 0.5) * vInstanceScale;
    gl_Position = uProjection * uView * vec4(worldPos, 0.0, 1.0);

    fUv = vQuadUV;
    fColor = vInstanceColor;
    fDimensions = vInstanceScale;
    fBorder = vInstanceBorder;
}