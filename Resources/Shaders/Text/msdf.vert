#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 vUv;
layout (location = 2) in vec4 vColor;
layout (location = 3) in float vPxRange;

uniform mat4 uProjection;
uniform mat4 uView;
uniform mat4 uModel;

out vec2 fUv;
out vec4 fColor;
out float fPxRange;

void main()
{
    fUv = vUv;
    fColor = vColor;
    fPxRange = vPxRange;
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
}