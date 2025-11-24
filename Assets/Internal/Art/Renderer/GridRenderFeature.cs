using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;


[Serializable]
public class GridPreset
{
    public int gridPlanSize = 4000;
    public int thresholdDistance = 40;
    [Range(0, 25)] public int divideFactor = 10;
    public bool useDistanceScaling = false;
}

public class GridRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    public Material gridMaterial;
    public GridPreset gridPreset;
		
    private GridRenderPass _gridRenderPass;
    private Vector3 _lastCameraPosition = Vector3.zero;

    public override void Create()
		{
			_gridRenderPass = new GridRenderPass()
			{
				renderPassEvent = renderPassEvent
			};
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			var camera = renderingData.cameraData.camera;
			
			if (_gridRenderPass is null || camera.name != "Main Camera")
				return;
			
			if(_gridRenderPass.GridMesh == false)
				_gridRenderPass.SetMesh(CreatePlaneMesh(gridPreset.gridPlanSize));

			if (_gridRenderPass.GridMaterial != gridMaterial)
				_gridRenderPass.SetMaterial(gridMaterial);
			
			var currentPosition = camera.transform.position;

			if (gridPreset.useDistanceScaling == true)
			{
				if (_lastCameraPosition != currentPosition)
				{
					var sizeRangeRatio = GetSizeRangeRatio(currentPosition.magnitude, gridPreset.thresholdDistance, gridPreset.divideFactor);
					_gridRenderPass.SetSizeRangeRatio(sizeRangeRatio);
					_lastCameraPosition = camera.transform.position;
				}
			}
			else
			{
				var fixedDivide = Mathf.Max(2, gridPreset.divideFactor);
				_gridRenderPass.SetSizeRangeRatio(new Vector3(4f, 100, 0.5f));
			}
			
			renderer.EnqueuePass(_gridRenderPass);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing == false)
				return;
			
			_gridRenderPass?.Dispose();
		}

		private Vector3 GetSizeRangeRatio(float distance, float thresholdDistance, int divide)
		{
			var power = 0;
			var distanceRatio = 0.0f;

			for (var i = 0; i < 100; ++i)
			{
				var distanceLimit = thresholdDistance * Mathf.Pow(divide, i);
				distanceRatio = distanceLimit == 0 ? 0 : distance / distanceLimit;
				
				power = i;
				
				if (Mathf.Floor(distanceRatio) == 0)
					break;
			}

			var baseFactor = power == 0 ? 1 : Mathf.Pow(divide, power);
			var divideFactor = power == 0 ? divide : Mathf.Pow(divide, power + 1);
			
			return new Vector3(baseFactor, divideFactor, distanceRatio);
		}

		private Mesh CreatePlaneMesh(int planeSize)
		{
			var vertices = new Vector3[4];
			vertices[0] = new Vector3(-planeSize, 0, -planeSize);
			vertices[1] = new Vector3(planeSize, 0, -planeSize);
			vertices[2] = new Vector3(-planeSize, 0, planeSize);
			vertices[3] = new Vector3(planeSize, 0, planeSize);
			
			var indices = new int[] { 0, 2, 1, 2, 3, 1 };
			
			var uv = new Vector2[4]
			{
				new Vector2(0, 0),
				new Vector2(1, 0),
				new Vector2(0, 1),
				new Vector2(1, 1)
			};

			var gridMesh = new Mesh
			{
				vertices = vertices,
				triangles =  indices,
				uv = uv
			};

			return gridMesh;
		}
}
