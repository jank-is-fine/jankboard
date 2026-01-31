#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D uscreenTexture;

void main()
{     
    vec4 originalColor = texture(uscreenTexture, TexCoords);    
    FragColor = originalColor;
}