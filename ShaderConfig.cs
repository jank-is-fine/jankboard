/// <summary>
/// <para>Configuration provider for all shader programs and post-processing render passes</para>
/// <para>Defines shader source mappings by name, loading vertex/fragment shaders from embedded resources</para>
/// <para>Provides structured access to shader definitions and render pass pipeline configuration</para>
/// <para>Used by ShaderManager for shader initialization and RenderHandler for post-processing setup</para>
/// </summary>

public static class ShaderConfig
{
    public static Dictionary<string, (string, string)> GetShaders()
    {
        return new()
        {
            {
                "Default Shader",
                (
                    ResourceHelper.GetEmbeddedText("DefaultVertShaderInvertedY.vert"),
                    ResourceHelper.GetEmbeddedText("DefaultTexture.frag")
                )
            },

            {
                "Batched Texture",
                (
                    ResourceHelper.GetEmbeddedText("BatchedTexture.vert"),
                    ResourceHelper.GetEmbeddedText("BatchedTexture.frag")
                )
            },

            {
                "nine-slice-batched",
                (
                    ResourceHelper.GetEmbeddedText("nine-slice-batched.vert"),
                    ResourceHelper.GetEmbeddedText("nine-slice-batched.frag")
                )
            },

            {
                "nine-slice",
                (
                    ResourceHelper.GetEmbeddedText("DefaultVertShader.vert"),
                    ResourceHelper.GetEmbeddedText("nine-slice.frag")
                )
            },

            {
                "connection-shader",
                (
                    ResourceHelper.GetEmbeddedText("ConnectionShader.vert"),
                    ResourceHelper.GetEmbeddedText("ConnectionShader.frag")
                )
            },

            {
                "Text Shader",
                (
                    ResourceHelper.GetEmbeddedText("msdf.vert"),
                    ResourceHelper.GetEmbeddedText("msdf.frag")
                )
            },

            {
                "Selection Shader",
                (
                    ResourceHelper.GetEmbeddedText("DefaultVertShader.vert"),
                    ResourceHelper.GetEmbeddedText("SingleColor.frag")
                )
            },

            {
                "Color-Picker Shader",
                (
                    ResourceHelper.GetEmbeddedText("DefaultVertShader.vert"),
                    ResourceHelper.GetEmbeddedText("ColorPickerShader.frag")
                )
            },

            {
                "Color-Picker-Degree Shader",
                (
                    ResourceHelper.GetEmbeddedText("DefaultVertShader.vert"),
                    ResourceHelper.GetEmbeddedText("ColorPickerDegreeShader.frag")
                )
            },

            {
                "Color-Alpha Shader",
                (
                    ResourceHelper.GetEmbeddedText("DefaultVertShader.vert"),
                    ResourceHelper.GetEmbeddedText("ColorPickerAlpha.frag")
                )
            },

            {
                "Outline Shader",
                (
                    ResourceHelper.GetEmbeddedText("OutlineEffect.vert"),
                    ResourceHelper.GetEmbeddedText("OutlineEffect.frag")
                )
            },

            {
                "DeuteranopeCorrectionShader",
                (
                    ResourceHelper.GetEmbeddedText("FullScreenVertexShader.vert"), 
                    ResourceHelper.GetEmbeddedText("DeuteranopeCorrectionShader.frag")  
                )
            },

            {
                "TritanopeCorrectionShader",
                (
                    ResourceHelper.GetEmbeddedText("FullScreenVertexShader.vert"),
                    ResourceHelper.GetEmbeddedText("TritanopeCorrectionShader.frag")
                )
            },

            {
                "ProtanopeCorrectionShader",
                (
                    ResourceHelper.GetEmbeddedText("FullScreenVertexShader.vert"),
                    ResourceHelper.GetEmbeddedText("ProtanopeCorrectionShader.frag")
                )
            },

            {
                "BlackAndWhiteShader",
                (
                    ResourceHelper.GetEmbeddedText("FullScreenVertexShader.vert"), 
                    ResourceHelper.GetEmbeddedText("BlackWhiteShader.frag") 
                )
            },

            {
                "FinalPassShader",
                (
                    ResourceHelper.GetEmbeddedText("FullScreenVertexShader.vert"), 
                    ResourceHelper.GetEmbeddedText("FinalPassShader.frag")  
                )
            },
        };
    }

    public static List<RenderPass> GetRenderPasses()
    {
        return
        [
            new TritanopeCorrectionRenderPass(),
            new ProtanopeCorrectionRenderPass(),
            new DeuteranopeCorrectionRenderPass(),
            new BlackAndWhiteRenderPass()
        ];
    }
}