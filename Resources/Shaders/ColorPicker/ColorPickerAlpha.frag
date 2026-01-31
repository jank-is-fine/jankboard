#version 330 core
in vec2 fUv;
out vec4 FragColor;

uniform vec4 uColor;

void main()
{
    float gradient = fUv.x;
    FragColor = vec4(gradient * uColor.r, gradient * uColor.g, gradient * uColor.b, 1.0);
}