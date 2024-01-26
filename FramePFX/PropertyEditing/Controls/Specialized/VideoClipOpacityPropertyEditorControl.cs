using System.Windows;
using System.Windows.Controls.Primitives;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Controls.Dragger;
using FramePFX.PropertyEditing.Specialized;

namespace FramePFX.PropertyEditing.Controls.Specialized {
    public class VideoClipOpacityPropertyEditorControl : BasePropEditControlContent {
        public VideoClipOpacityPropertyEditorSlot SlotModel => (VideoClipOpacityPropertyEditorSlot) base.Slot.Model;

        private NumberDragger opacitySlider;

        private readonly GetSetAutoPropertyBinder<VideoClipOpacityPropertyEditorSlot> opacityBinder = new GetSetAutoPropertyBinder<VideoClipOpacityPropertyEditorSlot>(RangeBase.ValueProperty, nameof(VideoClipOpacityPropertyEditorSlot.OpacityChanged), binder => binder.Model.Opacity, (binder, v) => binder.Model.SetValue((double) v));

        public VideoClipOpacityPropertyEditorControl() {
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            this.opacitySlider = this.GetTemplateChild<NumberDragger>("PART_OpacityDragger");
            this.opacitySlider.ValueChanged += (sender, args) => this.opacityBinder.OnControlValueChanged();
        }

        static VideoClipOpacityPropertyEditorControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VideoClipOpacityPropertyEditorControl), new FrameworkPropertyMetadata(typeof(VideoClipOpacityPropertyEditorControl)));
        }

        protected override void OnConnected() {
            this.opacityBinder.Attach(this.opacitySlider, this.SlotModel);
        }

        protected override void OnDisconnected() {
            this.opacityBinder.Detatch();
        }
    }
}