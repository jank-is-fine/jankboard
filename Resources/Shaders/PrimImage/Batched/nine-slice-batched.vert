#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 vUv;
layout (location = 2) in vec4 vColor;
layout (location = 3) in vec2 vDimensions;
layout (location = 4) in vec2 vBorder;

uniform mat4 uView;
uniform mat4 uProjection;

out vec2 fUv;
out vec4 fColor;
out vec2 fDimensions;
out vec2 fBorder;

void main()
{
    gl_Position = uProjection * uView * vec4(vPos, 1.0);
    fUv = vUv;
    fColor = vColor;
    fDimensions = vDimensions;
    fBorder = vBorder;
}