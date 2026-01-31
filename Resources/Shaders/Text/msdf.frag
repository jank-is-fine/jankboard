#version 330 core
in vec2 fUv;
in vec4 fColor;
in float fPxRange;

out vec4 FragColor;

uniform sampler2D uTexture0;

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

void main()
{
    vec4 mtsdf = texture(uTexture0, fUv);
    float msdDist = median(mtsdf.r, mtsdf.g, mtsdf.b) - 0.5;
    float tsdDist = mtsdf.a - 0.5;
    
    float sigDist = mix(msdDist, tsdDist, step(abs(tsdDist), abs(msdDist)));

    float screenPxRange = fPxRange * fwidth(sigDist);
    float opacity = clamp(sigDist / screenPxRange + 0.5, 0.0, 1.0);
    
    FragColor = vec4(fColor.rgb, fColor.a * opacity);
}