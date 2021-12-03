using System;

namespace UnityEngine.Rendering.Universal.Internal
{
    public class DepthNormalOnlyPass : ScriptableRenderPass
    {
        internal RenderTextureDescriptor normalDescriptor { get; private set; }
        internal RenderTextureDescriptor depthDescriptor { get; private set; }
        private RenderTargetHandle depthHandle { get; set; }
        private RenderTargetHandle normalHandle { get; set; }
        private ShaderTagId m_ShaderTagId = new ShaderTagId("DepthNormals");
        private FilteringSettings m_FilteringSettings;
        private const int k_DepthBufferBits = 32;
        public DepthNormalOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DepthNormalOnlyPass));
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
        }
        /// <summary>
        /// Setup
        /// </summary>
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthHandle, RenderTargetHandle normalHandle)
        {
            this.depthHandle = depthHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = k_DepthBufferBits;
            baseDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
            depthDescriptor = baseDescriptor;

            this.normalHandle = normalHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.RGHalf;
            baseDescriptor.depthBufferBits = 0;
            baseDescriptor.msaaSamples = 1;
            normalDescriptor = baseDescriptor;
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            cmd.GetTemporaryRT(normalHandle.id, normalDescriptor, FilterMode.Point);
            cmd.GetTemporaryRT(depthHandle.id, depthDescriptor, FilterMode.Point);
            ConfigureTarget(new RenderTargetIdentifier(normalHandle.Identifier(), 0, CubemapFace.Unknown, -1),
                new RenderTargetIdentifier(depthHandle.Identifier(), 0, CubemapFace.Unknown, -1));
            ConfigureClear(ClearFlag.All, Color.black);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.DepthNormalPrepass)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;
                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }
            if (depthHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(normalHandle.id);
                cmd.ReleaseTemporaryRT(depthHandle.id);
                normalHandle = RenderTargetHandle.CameraTarget;
                depthHandle = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
