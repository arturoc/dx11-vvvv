﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;
using SlimDX;
using VVVV.Utils.VMath;

using VVVV.DX11.Lib.Devices;
using SlimDX.Direct3D11;
using System.ComponentModel.Composition;
using VVVV.Hosting.Pins;
using VVVV.DX11.Internals.Helpers;
using VVVV.DX11.Internals;
using VVVV.DX11.Internals.Effects;

using VVVV.DX11.Lib.Rendering;
using FeralTic.DX11.Queries;
using FeralTic.DX11.Resources;
using FeralTic.DX11;
using System.IO;
using FeralTic.DX11.Utils;

namespace VVVV.DX11.Nodes
{
    [PluginInfo(Name = "Renderer", Category = "DX11", Version = "StreamOut", Author = "vux", AutoEvaluate = false)]
    public class DX11SORendererNode : IPluginEvaluate, IDX11RendererProvider, IDisposable
    {
        protected IPluginHost FHost;

        [Input("Layer", Order = 1, IsSingle = true)]
        protected Pin<DX11Resource<DX11Layer>> FInLayer;

        [Input("Vertex Size", Order = 8, DefaultValue = 12)]
        protected IDiffSpread<int> FInVSize;

        [Input("Element Count", Order = 8, DefaultValue = 512)]
        protected IDiffSpread<int> FInElemCount;

        [Input("Output Layout", Order = 10005, CheckIfChanged = true)]
        protected Pin<InputElement> FInLayout;

        [Input("Reset Counter Value")]
        protected IDiffSpread<int> FInResetCounterValue;

        [Input("Enabled", DefaultValue = 1, Order = 15)]
        protected ISpread<bool> FInEnabled;

        [Input("View", Order = 16)]
        protected IDiffSpread<Matrix> FInView;

        [Input("Projection", Order = 17)]
        protected IDiffSpread<Matrix> FInProjection;

        [Output("Geometry Out", IsSingle = true)]
        protected ISpread<DX11Resource<IDX11Geometry>> FOutGeom;

        protected int vsize;
        protected int cnt;

        protected List<DX11RenderContext> updateddevices = new List<DX11RenderContext>();
        protected List<DX11RenderContext> rendereddevices = new List<DX11RenderContext>();

        private bool reset = false;


        public event DX11QueryableDelegate BeginQuery;

        public event DX11QueryableDelegate EndQuery;

        private DX11RenderSettings settings = new DX11RenderSettings();
        private SlimDX.Direct3D11.Buffer buffer;

        [ImportingConstructor()]
        public DX11SORendererNode(IPluginHost FHost)
        {
            //this.settings.CustomSemantics.Add(this.rwbuffersemantic);
        }

        public void Evaluate(int SpreadMax)
        {
            this.rendereddevices.Clear();
            this.updateddevices.Clear();

            reset = this.FInVSize.IsChanged || this.FInElemCount.IsChanged;

            if (this.FOutGeom[0] == null)
            {
                this.FOutGeom[0] = new DX11Resource<IDX11Geometry>();
            }

            if (reset)
            {
                this.cnt = this.FInElemCount[0];
                this.vsize = this.FInVSize[0];
            }
        }

        public bool IsEnabled
        {
            get { return this.FInEnabled[0]; }
        }

        public void Render(DX11RenderContext context)
        {
            Device device = context.Device;
            DeviceContext ctx = context.CurrentDeviceContext;

            //Just in case
            if (!this.updateddevices.Contains(context))
            {
                this.Update(null, context);
            }



            if (!this.FInLayer.PluginIO.IsConnected) { return; }

            if (this.rendereddevices.Contains(context)) { return; }

            if (this.FInEnabled[0])
            {
                if (this.BeginQuery != null)
                {
                    this.BeginQuery(context);
                }

                context.CurrentDeviceContext.OutputMerger.SetTargets(new RenderTargetView[0]);

                ctx.StreamOutput.SetTargets(new StreamOutputBufferBinding(this.buffer, 0));

                int rtmax = Math.Max(this.FInProjection.SliceCount, this.FInView.SliceCount);

                for (int i = 0; i < rtmax; i++)
                {
                    settings.ViewportIndex = 0;
                    settings.ViewportCount = 1;
                    settings.View = this.FInView[i];
                    settings.Projection = this.FInProjection[i];
                    settings.ViewProjection = settings.View * settings.Projection;
                    settings.RenderWidth = this.cnt;
                    settings.RenderHeight = this.cnt;
                    settings.RenderDepth = this.cnt;
                    settings.BackBuffer = null;

                    // this.rwbuffersemantic.Data = this.FOutBuffers[0][context];

                    for (int j = 0; j < this.FInLayer.SliceCount; j++)
                    {
                        this.FInLayer[j][context].Render(this.FInLayer.PluginIO, context, settings);
                    }
                }

                ctx.StreamOutput.SetTargets(null);

            }
        }

        public void Update(IPluginIO pin, DX11RenderContext context)
        {
            if (this.updateddevices.Contains(context)) { return; }
            if (reset || !this.FOutGeom[0].Contains(context))
            {
                this.DisposeBuffers(context);

                // int vsize = customlayout ? size : ig.VertexSize;
                SlimDX.Direct3D11.Buffer vbo = BufferHelper.CreateStreamOutBuffer(context, vsize, this.cnt);

                //Copy a new Vertex buffer with stream out
                DX11VertexGeometry vg = new DX11VertexGeometry(context);
                vg.AssignDrawer(new DX11VertexAutoDrawer());
                vg.HasBoundingBox = false;
                vg.InputLayout = this.FInLayout.ToArray();
                vg.Topology = PrimitiveTopology.TriangleList;
                vg.VertexBuffer = vbo;
                vg.VertexSize = vsize;
                vg.VerticesCount = this.cnt;

                this.buffer = vbo;

                this.FOutGeom[0][context] = vg;
            }

            this.updateddevices.Add(context);
        }

        public void Destroy(IPluginIO pin, DX11RenderContext OnDevice, bool force)
        {
            this.DisposeBuffers(OnDevice);
        }

        #region Dispose Buffers
        private void DisposeBuffers(DX11RenderContext context)
        {
            for (int i = 0; i < this.FOutGeom.SliceCount; i++)
            {
                this.FOutGeom[i].Dispose(context);
            }
        }
        #endregion

        public void Dispose()
        {
            for (int i = 0; i < this.FOutGeom.SliceCount; i++)
            {
                this.FOutGeom[i].Dispose();
            }
        }
    }
}
