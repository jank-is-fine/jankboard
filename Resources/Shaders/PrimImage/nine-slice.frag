#version 330 core
in vec2 fUv;
out vec4 FragColor;

uniform sampler2D uTexture0;
uniform vec4 uColor;
uniform vec2 uDimensions;
uniform vec2 uBorderSize;


float map(float value, float min1, float max1, float min2, float max2)
{
    return (value - min1) / (max1 - min1) * (max2 - min2) + min2;
}


float processAxis(float coord, float dimension, float borderPixels)
{
    float borderUV = borderPixels / dimension;
    
    if (coord < borderUV)
    {
        return map(coord, 0.0, borderUV, 0.0, 0.33);
    }

    if (coord < 1.0 - borderUV) 
    {
        return map(coord, borderUV, 1.0 - borderUV, 0.33, 0.67);
    }
    
    return map(coord, 1.0 - borderUV, 1.0, 0.67, 1.0);
}

void main()
{
    vec2 newUV = vec2(
        processAxis(fUv.x, uDimensions.x, uBorderSize.x),
        processAxis(fUv.y, uDimensions.y, uBorderSize.y)
    );

    vec4 textureColor = texture(uTexture0, newUV);
    FragColor = textureColor * uColor;
}