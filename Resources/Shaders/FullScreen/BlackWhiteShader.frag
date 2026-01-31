#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D uscreenTexture;

vec3 rgbToGrayscale(vec3 color)
{
    float luminance = dot(color, vec3(0.299, 0.587, 0.114));
    return vec3(luminance);
}

void main()
{     
    vec3 originalColor = texture(uscreenTexture, TexCoords).rgb;        
    FragColor = vec4(rgbToGrayscale(originalColor), 1.0);
}