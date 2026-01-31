#version 330 core
in vec2 fUv;
out vec4 FragColor;

uniform sampler2D uTexture0;
uniform vec4 uColor;
uniform vec4 uPickerColor;

void main()
{
    vec4 texColor = texture(uTexture0, fUv);
    
    float saturation = fUv.x;
    float value = fUv.y;
    
    vec3 color = mix(vec3(1.0, 1.0, 1.0), uPickerColor.rgb, saturation);
    
    color *= value;
    
    FragColor = vec4(color * texColor.rgb * uColor.rgb, uColor.a);
}