#version 330 core
in vec2 fUv;
in vec4 fColor;

out vec4 FragColor;

uniform sampler2D uTexture0;

void main()
{
    vec4 texColor = texture(uTexture0, fUv);
    FragColor = texColor * fColor;
}