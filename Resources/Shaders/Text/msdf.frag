#version 330 core

in vec2 fUv;
in vec4 fColor;
in float fPxRange;

out vec4 FragColor;

uniform sampler2D uTexture0;

float median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

void main()
{
    vec4 mtsdf = texture(uTexture0, fUv);

    float msdf = median(mtsdf.r, mtsdf.g, mtsdf.b) - 0.5;
    float sdf  = mtsdf.a - 0.5;

    float blend = smoothstep(0.0, 0.02, abs(msdf) - abs(sdf));
    float dist = mix(msdf, sdf, blend);

    vec2 unitRange = vec2(fPxRange) / vec2(textureSize(uTexture0, 0));
    vec2 screenTexSize = vec2(1.0) / fwidth(fUv);

    float screenPxRange =
        max(0.5 * dot(unitRange, screenTexSize), 1.0);

    float opacity =
        clamp(screenPxRange * dist + 0.5, 0.0, 1.0);

    FragColor = vec4(fColor.rgb, fColor.a * opacity);
}