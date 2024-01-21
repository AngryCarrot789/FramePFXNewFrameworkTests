using System;
using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.ResourceManaging;

namespace FramePFX.Editors.Controls.Resources {
    /// <summary>
    /// The main control which manages the UI for the resource manager and all
    /// of the resources inside of it, such as a resource tree, 'current folder' list, selection, etc.
    /// </summary>
    public class ResourcePanelControl : Control {
        public static readonly DependencyProperty ResourceManagerProperty =
            DependencyProperty.Register(
                "ResourceManager",
                typeof(ResourceManager),
                typeof(ResourcePanelControl),
                new PropertyMetadata(null, (d, e) => ((ResourcePanelControl) d).OnResourceManagerChanged((ResourceManager) e.OldValue, (ResourceManager) e.NewValue)));

        public ResourceManager ResourceManager {
            get => (ResourceManager) this.GetValue(ResourceManagerProperty);
            set => this.SetValue(ResourceManagerProperty, value);
        }

        public ResourceListControl ResourceList { get; private set; }

        public ResourcePanelControl() {

        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!(this.GetTemplateChild("PART_ResourceList") is ResourceListControl listBox))
                throw new Exception("Missing PART_ResourceList");
            this.ResourceList = listBox;
        }

        // Assuming OnApplyTemplate is called before this method, which appears the be every time
        private void OnResourceManagerChanged(ResourceManager oldManager, ResourceManager newManager) {
            if (oldManager != null) {
            }

            this.ResourceList.ResourceManager = newManager;
            if (newManager != null) {

            }
        }
    }
}