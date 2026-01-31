#version 330 core
in vec2 fUv;
out vec4 FragColor;

uniform sampler2D uTexture0;
uniform vec4 uColor;

//https://gist.github.com/983/e170a24ae8eba2cd174f
vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main()
{
    float hue = fUv.x * 360.0;
    
    vec3 rainbowColor = hsv2rgb(vec3(hue / 360.0, 1.0, 1.0));
    
    vec4 texColor = texture(uTexture0, fUv);
    FragColor = vec4(rainbowColor * texColor.rgb * uColor.rgb, uColor.a);
}