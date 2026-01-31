#version 330 core

in vec2 fUv;
in vec4 fColor;
out vec4 FragColor;

uniform float uTime;
uniform sampler2D uNoiseTexture;

void main()
{
    vec2 particleUV = fUv * 3 + vec2(uTime * 0.8, uTime * 0.6);
    float noise1 = texture(uNoiseTexture, particleUV).r;
    
    vec2 particleUV2 = fUv * 3 - vec2(uTime * 0.4, uTime * 0.5);
    float noise2 = texture(uNoiseTexture, particleUV2).r;
    
    float blob = smoothstep(0.4, 0.8, (noise1 + noise2) * 0.5);
    
    float alpha = fColor.a * blob;
    
    FragColor = vec4(fColor.rgb, alpha + fColor.a/2);
}