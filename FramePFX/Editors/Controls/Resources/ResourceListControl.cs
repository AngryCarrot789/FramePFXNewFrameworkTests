
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using FramePFX.Editors.ResourceManaging;
using FramePFX.Editors.ResourceManaging.Events;
using FramePFX.Interactivity;

namespace FramePFX.Editors.Controls.Resources {
    public class ResourceListControl : MultiSelector {
        public static readonly DependencyProperty ResourceManagerProperty = DependencyProperty.Register("ResourceManager", typeof(ResourceManager), typeof(ResourceListControl), new PropertyMetadata(null, (d, e) => ((ResourceListControl) d).OnResourceManagerChanged((ResourceManager) e.OldValue, (ResourceManager) e.NewValue)));
        public static readonly DependencyProperty CurrentFolderProperty = DependencyProperty.Register("CurrentFolder", typeof(ResourceFolder), typeof(ResourceListControl), new PropertyMetadata(null, (d, e) => ((ResourceListControl) d).OnCurrentFolderChanged((ResourceFolder) e.OldValue, (ResourceFolder) e.NewValue)));
        public const string ResourceDropType = "PFXResource_DropType";

        public ResourceManager ResourceManager {
            get => (ResourceManager) this.GetValue(ResourceManagerProperty);
            set => this.SetValue(ResourceManagerProperty, value);
        }

        /// <summary>
        /// Gets or sets the folder that this control is currently displaying the contents of.
        /// This may affect the <see cref="ItemsControl.Items"/> collection
        /// </summary>
        public ResourceFolder CurrentFolder {
            get => (ResourceFolder) this.GetValue(CurrentFolderProperty);
            set => this.SetValue(CurrentFolderProperty, value);
        }

        private const int MaxItemCacheSize = 64;
        private const int MaxItemContentCacheSize = 16;
        private readonly Stack<ResourceListItemControl> itemCache;
        private readonly Dictionary<Type, Stack<ResourceListItemContentControl>> itemContentCacheMap;
        private bool isProcessingAsyncDrop;

        public ResourceListItemControl lastSelectedItem;

        public ResourceListControl() {
            this.itemCache = new Stack<ResourceListItemControl>(MaxItemCacheSize);
            this.itemContentCacheMap = new Dictionary<Type, Stack<ResourceListItemContentControl>>();
            this.AllowDrop = true;
            this.CanSelectMultipleItems = true;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            if (this.Items.Cast<ResourceListItemControl>().Any(x => x.ResourceList == this && x.IsMouseOver)) {
                return;
            }

            this.UnselectAll();
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseDown(e);
            if (e.ChangedButton == MouseButton.XButton1) {
                // Go backwards in history
                this.CurrentFolder = this.CurrentFolder?.Parent ?? this.ResourceManager?.RootFolder;
            }
            // else if (e.ChangedButton == MouseButton.XButton2) {
            //     this.ResourceManager?.GoForward();
            // }
        }
        
        public void MakeRangedSelection(ResourceListItemControl a, ResourceListItemControl b) {
            if (a == b) {
                this.MakePrimarySelection(a);
            }
            else {
                int indexA = this.ItemContainerGenerator.IndexFromContainer(a);
                if (indexA == -1) {
                    return;
                }

                int indexB = this.ItemContainerGenerator.IndexFromContainer(b);
                if (indexB == -1) {
                    return;
                }

                if (indexA < indexB) {
                    this.UnselectAll();
                    for (int i = indexA; i <= indexB; i++) {
                        this.SetItemSelectedPropertyAtIndex(i, true);
                    }
                }
                else if (indexA > indexB) {
                    this.UnselectAll();
                    for (int i = indexB; i <= indexA; i++) {
                        this.SetItemSelectedPropertyAtIndex(i, true);
                    }
                }
                else {
                    this.SetItemSelectedPropertyAtIndex(indexA, true);
                }
            }
        }

        public void MakePrimarySelection(ResourceListItemControl item) {
            this.UnselectAll();
            this.SetItemSelectedProperty(item, true);
            this.lastSelectedItem = item;
        }

        public void SetItemSelectedProperty(ResourceListItemControl item, bool selected) {
            item.IsSelected = selected;
            object x = this.ItemContainerGenerator.ItemFromContainer(item);
            if (x == null || x == DependencyProperty.UnsetValue)
                x = item;

            if (selected) {
                this.SelectedItems.Add(x);
            }
            else {
                this.SelectedItems.Remove(x);
            }
        }

        public bool SetItemSelectedPropertyAtIndex(int index, bool selected) {
            if (index < 0 || index >= this.Items.Count) {
                return false;
            }

            if (this.ItemContainerGenerator.ContainerFromIndex(index) is ResourceListItemControl resource) {
                this.SetItemSelectedProperty(resource, selected);
                return true;
            }
            else {
                return false;
            }
        }

        protected override void OnDragEnter(DragEventArgs e) => this.OnDragOver(e);

        protected override void OnDragOver(DragEventArgs e) {
            ResourceFolder currentFolder;
            if (this.isProcessingAsyncDrop || (currentFolder = this.CurrentFolder) == null) {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            ResourceListItemControl.ProcessCanDragOver(currentFolder, e);
        }

        protected override async void OnDrop(DragEventArgs e) {
            e.Handled = true;
            if (this.isProcessingAsyncDrop || !(this.CurrentFolder is ResourceFolder currentFolder)) {
                return;
            }

            try {
                this.isProcessingAsyncDrop = true;
                if (ResourceListItemControl.GetDropResourceListForEvent(e, out List<BaseResource> list, out EnumDropType effects)) {
                    await ResourceDropRegistry.DropRegistry.OnDropped(currentFolder, list, effects);
                }
                else if (!await ResourceDropRegistry.DropRegistry.OnDroppedNative(currentFolder, new DataObjectWrapper(e.Data), effects)) {
                    MessageBox.Show("Unknown dropped item. Drop files here", "Unknown data");
                    // await IoC.DialogService.ShowMessageAsync("Unknown data", "Unknown dropped item. Drop files here");
                }
            }
            finally {
                this.isProcessingAsyncDrop = false;
            }
        }

        private void OnCurrentFolderChanged(ResourceFolder oldFolder, ResourceFolder newFolder) {
            if (oldFolder != null) {
                oldFolder.ResourceAdded -= this.CurrentFolder_OnResourceAdded;
                oldFolder.ResourceRemoved -= this.CurrentFolder_OnResourceRemoved;
                oldFolder.ResourceMoved -= this.CurrentFolder_OnResourceMoved;
                this.ClearResourcesInternal();
            }

            if (newFolder != null) {
                newFolder.ResourceAdded += this.CurrentFolder_OnResourceAdded;
                newFolder.ResourceRemoved += this.CurrentFolder_OnResourceRemoved;
                newFolder.ResourceMoved += this.CurrentFolder_OnResourceMoved;
                int i = 0;
                foreach (BaseResource resource in newFolder.Items) {
                    this.InsertResourceInternal(resource, i++);
                }
            }
        }

        private void CurrentFolder_OnResourceAdded(ResourceFolder parent, BaseResource item, int index) {
            this.InsertResourceInternal(item, index);
        }

        private void CurrentFolder_OnResourceRemoved(ResourceFolder parent, BaseResource item, int index) {
            this.RemoveResourceInternal(index);
        }

        private void CurrentFolder_OnResourceMoved(ResourceFolder sender, ResourceMovedEventArgs e) {
            if (e.IsSameFolder) { // Item was moved within the current folder itself
                this.MoveResourceInternal(e.OldIndex, e.NewIndex);
            }
            else if (e.NewFolder == sender) { // It was effectively added
                this.InsertResourceInternal(e.Item, e.NewIndex);
            }
            else { // It was effectively removed
                this.RemoveResourceInternal(e.OldIndex);
            }
        }

        private void OnResourceManagerChanged(ResourceManager oldManager, ResourceManager newManager) {
            if (oldManager != null) {
                this.CurrentFolder = null;
            }

            if (newManager != null) {
                this.CurrentFolder = newManager.RootFolder;
            }
        }

        private void ClearResourcesInternal() {
            for (int i = this.Items.Count - 1; i >= 0; i--) {
                this.RemoveResourceInternal(i);
            }
        }

        private void InsertResourceInternal(BaseResource resource, int index) {
            ResourceListItemControl control = this.itemCache.Count > 0 ? this.itemCache.Pop() : new ResourceListItemControl();
            control.OnAddingToList(this, resource);
            this.Items.Insert(index, control);
            control.OnAddedToList();
            control.InvalidateMeasure();
            this.InvalidateMeasure();
        }

        private void RemoveResourceInternal(int index) {
            ResourceListItemControl control = (ResourceListItemControl) this.Items[index];
            control.OnRemovingFromList();
            this.Items.RemoveAt(index);
            control.OnRemovedFromList();
            if (this.itemCache.Count < MaxItemCacheSize)
                this.itemCache.Push(control);
            this.InvalidateMeasure();
        }

        private void MoveResourceInternal(int oldIndex, int newIndex) {
            ResourceListItemControl control = (ResourceListItemControl) this.Items[oldIndex];
            // control.OnIndexMoving(oldIndex, newIndex);
            this.Items.RemoveAt(oldIndex);
            this.Items.Insert(newIndex, control);
            // control.OnIndexMoved(oldIndex, newIndex);
            this.InvalidateMeasure();
        }

        /// <summary>
        /// Either returns a cached content object from resource type, or creates a new instance of it.
        /// <see cref="ReleaseContentObject"/> should be called after the returned object is no longer needed,
        /// in order to help with performance (saves re-creating the object and applying styles)
        /// </summary>
        /// <param name="resourceType">The resource object type</param>
        /// <returns>A reused or new content object</returns>
        public ResourceListItemContentControl GetContentObject(Type resourceType) {
            ResourceListItemContentControl content;
            if (this.itemContentCacheMap.TryGetValue(resourceType, out Stack<ResourceListItemContentControl> stack) && stack.Count > 0) {
                content = stack.Pop();
            }
            else {
                content = ResourceListItemContentControl.NewInstance(resourceType);
            }

            return content;
        }

        /// <summary>
        /// Adds the given content object to our internal cache (keyed by the given resource type) if the cache
        /// is small enough, otherwise the object is forgotten and garbage collected (at least, that's the intent;
        /// bugs in the disconnection code may prevent that).
        /// The content object should not be used after this call, instead use <see cref="GetContentObject"/>
        /// </summary>
        /// <param name="resourceType">The resource object type</param>
        /// <param name="contentControl">The content object type that is no longer in use</param>
        /// <returns>True when the object was cached, false when the cache is too large and could not fit the object in</returns>
        public bool ReleaseContentObject(Type resourceType, ResourceListItemContentControl contentControl) {
            if (!this.itemContentCacheMap.TryGetValue(resourceType, out Stack<ResourceListItemContentControl> stack)) {
                this.itemContentCacheMap[resourceType] = stack = new Stack<ResourceListItemContentControl>();
            }
            else if (stack.Count > MaxItemContentCacheSize) {
                return false;
            }

            stack.Push(contentControl);
            return true;
        }

        public IEnumerable<ResourceListItemControl> GetSelectedControls() {
            return this.SelectedItems.Cast<ResourceListItemControl>();
        }

        public IEnumerable<BaseResource> GetSelectedResources() {
            foreach (ResourceListItemControl resource in this.GetSelectedControls()) {
                if (resource.Model != null) {
                    yield return resource.Model;
                }
            }
        }
    }
}