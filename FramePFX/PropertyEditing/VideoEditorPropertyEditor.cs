using FramePFX.Editors.Timelines.Clips;
using FramePFX.PropertyEditing.Specialized;

namespace FramePFX.PropertyEditing {
    /// <summary>
    /// A class which stores the video editor's general property editor information
    /// </summary>
    public class VideoEditorPropertyEditor {
        public static VideoEditorPropertyEditor Instance { get; } = new VideoEditorPropertyEditor();

        public FixedPropertyEditorGroup Root { get; }

        private FixedPropertyEditorGroup ClipGroup { get; }

        private VideoEditorPropertyEditor() {
            this.Root = new FixedPropertyEditorGroup(typeof(object)) {
                DisplayName = "Root Object", IsExpanded = true
            };

            {
                this.ClipGroup = new FixedPropertyEditorGroup(typeof(Clip)) {
                    DisplayName = "Clip", IsExpanded = true
                };

                this.ClipGroup.AddItem(new ClipDisplayNamePropertyEditorSlot());
                this.ClipGroup.AddItem(new VideoClipOpacityPropertyEditorSlot());
            }

            this.Root.AddItem(this.ClipGroup);
        }
    }
}