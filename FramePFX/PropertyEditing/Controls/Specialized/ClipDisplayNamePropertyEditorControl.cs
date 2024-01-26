using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Controls.Binders;
using FramePFX.PropertyEditing.Specialized;

namespace FramePFX.PropertyEditing.Controls.Specialized {
    public class ClipDisplayNamePropertyEditorControl : BasePropEditControlContent {
        public ClipDisplayNamePropertyEditorSlot SlotModel => (ClipDisplayNamePropertyEditorSlot) base.Slot.Model;

        private TextBox displayNameBox;

        private readonly GetSetAutoPropertyBinder<ClipDisplayNamePropertyEditorSlot> displayNameBinder = new GetSetAutoPropertyBinder<ClipDisplayNamePropertyEditorSlot>(TextBox.TextProperty, nameof(ClipDisplayNamePropertyEditorSlot.DisplayNameChanged), binder => binder.Model.DisplayName, (binder, v) => binder.Model.SetValue((string) v));

        public ClipDisplayNamePropertyEditorControl() {
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            this.displayNameBox = this.GetTemplateChild<TextBox>("PART_TextBox");
            this.displayNameBox.TextChanged += (sender, args) => this.displayNameBinder.OnControlValueChanged();
        }

        static ClipDisplayNamePropertyEditorControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ClipDisplayNamePropertyEditorControl), new FrameworkPropertyMetadata(typeof(ClipDisplayNamePropertyEditorControl)));
        }

        protected override void OnConnected() {
            this.displayNameBinder.Attach(this.displayNameBox, this.SlotModel);
        }

        protected override void OnDisconnected() {
            this.displayNameBinder.Detatch();
        }
    }
}