using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class GridRenderPass : ScriptableRenderPass
{
    public Material GridMaterial { get; private set; }
    public Mesh GridMesh { get; private set; }

    private readonly int _baseFactorID = Shader.PropertyToID("_BaseFactor");
    private readonly int _divideFactorID = Shader.PropertyToID("_DivideFactor");
    private readonly int _distanceRatioID = Shader.PropertyToID("_DistanceRatio");

    private class GridRenderData
    {
        public Material material;
        public Mesh mesh;
    }
    
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        using (var builder = renderGraph.AddRasterRenderPass<GridRenderData>(nameof(GridRenderPass), out var passData))
        {
            passData.material = GridMaterial;
            passData.mesh = GridMesh;
				
            var resourceData = frameData.Get<UniversalResourceData>();
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
				
            builder.SetRenderFunc((GridRenderData data, RasterGraphContext rgContext) =>
            {
                ExecutePass(data, rgContext);
            });
        }
    }
		
    static void ExecutePass(GridRenderData data, RasterGraphContext rgContext)
    {
        if (data.mesh == false || data.material == false)
            return;
            
        rgContext.cmd.DrawMesh(data.mesh, Matrix4x4.identity, data.material);
    }
		
    public void SetSizeRangeRatio(Vector3 sizeRangeRatio)
    {
        if (GridMaterial == false)
            return;
			
        GridMaterial.SetFloat(_baseFactorID, sizeRangeRatio.x);
        GridMaterial.SetFloat(_divideFactorID, sizeRangeRatio.y);
        GridMaterial.SetFloat(_distanceRatioID, sizeRangeRatio.z);
    }

    public void Dispose()
    {
        GridMaterial = null;
        GridMesh = null;
    }

    public void SetMesh(Mesh mesh)
    {
        GridMesh = mesh;
    }

    public void SetMaterial(Material material)
    {
        GridMaterial = material;
    }
}
