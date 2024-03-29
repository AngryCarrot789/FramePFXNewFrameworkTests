using FramePFX.Editors.Rendering;
using FramePFX.Editors.ResourceManaging;
using FramePFX.Editors.ResourceManaging.ResourceHelpers;
using FramePFX.Editors.ResourceManaging.Resources;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Clips {
    public class ImageVideoClip : VideoClip {
        public IResourcePathKey<ResourceImage> ResourceImageKey { get; }

        private volatile SKImage renderImage;
        private volatile int disposeImage;
        private SKFilterQuality renderQuality = SKFilterQuality.Medium;
        private double renderOpacity;
        private readonly object renderLock;
        private volatile bool isRendering;

        public ImageVideoClip() {
            this.ResourceImageKey = this.ResourceHelper.RegisterKeyByTypeName<ResourceImage>();
            this.ResourceImageKey.ResourceChanged += this.OnResoureChanged;
            this.renderLock = new object();
        }

        private void SignalDisposeImageOnRender() {
            lock (this.renderLock) {
                if (this.isRendering) {
                    this.disposeImage = 1;
                }
            }
        }

        private void OnResoureChanged(IResourcePathKey<ResourceImage> key, ResourceImage olditem, ResourceImage newitem) {
            this.SignalDisposeImageOnRender();
            if (olditem != null)
                olditem.ImageChanged -= this.OnImageChanged;
            if (newitem != null)
                newitem.ImageChanged += this.OnImageChanged;
        }

        private void OnImageChanged(BaseResource resource) {
            this.SignalDisposeImageOnRender();
        }

        public override void PrepareRenderFrame(PreRenderContext ctx, long frame) {
            this.renderOpacity = this.Opacity;
            if (this.ResourceImageKey.TryGetResource(out ResourceImage resource)) {
                this.renderImage = resource.image;
            }
        }

        public override void RenderFrame(RenderContext rc) {
            SKImage image;
            lock (this.renderLock) {
                if ((image = this.renderImage) == null) {
                    return;
                }

                this.isRendering = true;
            }

            using (SKPaint paint = new SKPaint {FilterQuality = this.renderQuality, ColorF = RenderUtils.BlendAlpha(SKColors.White, this.renderOpacity)})
                rc.Canvas.DrawImage(image, 0, 0, paint);

            lock (this.renderLock) {
                if (this.disposeImage != 0) {
                    this.disposeImage = 0;
                    image.Dispose();
                }

                this.renderImage = null;
                this.isRendering = false;
            }
        }
    }
}