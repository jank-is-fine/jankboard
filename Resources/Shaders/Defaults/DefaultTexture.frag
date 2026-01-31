#version 330 core
in vec2 fUv;
out vec4 FragColor;

uniform sampler2D uTexture0;
uniform vec4 uColor;

void main()
{
    vec4 texColor = texture(uTexture0, fUv);
    FragColor = vec4(texColor * uColor);
}