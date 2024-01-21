using System;
using System.Runtime.CompilerServices;
using FramePFX.Editors.Factories;
using FramePFX.Editors.ResourceManaging.Events;
using FramePFX.RBC;

namespace FramePFX.Editors.ResourceManaging {
    public delegate void BaseResourceEventHandler(BaseResource resource);

    /// <summary>
    /// Base class for resource items and groups
    /// </summary>
    public abstract class BaseResource {
        internal ResourceManager manager;
        private string displayName;

        /// <summary>
        /// The manager that this resource belongs to. Null if <see cref="Parent"/> is null, or there is just no manager associated with this hierarchy
        /// </summary>
        public ResourceManager Manager => this.manager;

        /// <summary>
        /// The folder that this object is currently in. Null if we aren't in a folder
        /// </summary>
        public ResourceFolder Parent { get; private set; }

        /// <summary>
        /// This resource object's registry ID, used to reflectively create an instance of it while deserializing data
        /// </summary>
        public string FactoryId => ResourceTypeFactory.Instance.GetId(this.GetType());

        public string DisplayName {
            get => this.displayName;
            set {
                if (this.displayName == value)
                    return;
                this.displayName = value;
                this.DisplayNameChanged?.Invoke(this);
                this.OnDataModified(nameof(this.DisplayName));
            }
        }

        public event ResourceModifiedEventHandler DataModified;
        public event BaseResourceEventHandler DisplayNameChanged;

        protected BaseResource() {
        }

        /// <summary>
        /// Creates a clone of the item, and also any child items if the item is a group. This will not register
        /// anything with the resource manager, that must be done manually or via <see cref="CloneAndRegister"/>
        /// </summary>
        /// <param name="item">The item to clone</param>
        /// <returns>A cloned and fully registered but offline resource</returns>
        /// <exception cref="Exception">Internal error with the resource registry; cloned item type does not match the original item</exception>
        public static BaseResource Clone(BaseResource item) {
            BaseResource clone = ResourceTypeFactory.Instance.NewResource(item.FactoryId);
            if (clone.GetType() != item.GetType())
                throw new Exception("Cloned object type does not match the item type");
            item.LoadDataIntoClone(clone);
            return clone;
        }

        /// <summary>
        /// Clones the item and registers the clone and any child items with the given item's resource manager, if available
        /// </summary>
        /// <param name="item">The item to clone</param>
        /// <returns>A cloned and fully registered but offline resource</returns>
        /// <exception cref="Exception">Internal error with the resource registry; cloned item type does not match the original item</exception>
        public static BaseResource CloneAndRegister(BaseResource item) {
            BaseResource clone = Clone(item);
            if (item.Manager != null)
                ResourceFolder.RegisterHierarchy(item.Manager, clone);
            return clone;
        }

        protected static void InternalSetParent(BaseResource obj, ResourceFolder parent) {
            obj.Parent = parent;
        }

        /// <summary>
        /// Raises the <see cref="DataModified"/> event for this resource item with the given property, allowing listeners
        /// to invalidate their objects that relied on the previous state of the property that changed e.g. text blobs)
        /// </summary>
        /// <param name="propertyName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual void OnDataModified([CallerMemberName] string propertyName = null) {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            this.DataModified?.Invoke(this, propertyName);
        }

        /// <summary>
        /// Invoked when this resource is added to a resource manager's resource hierarchy.
        /// <see cref="Manager"/> is set to a non-null value prior to this call
        /// </summary>
        public virtual void OnAttachedToManager() {

        }

        /// <summary>
        /// Invoked when this resource is removed from a resource manager's resource hierarchy.
        /// <see cref="Manager"/> is set to null after this call
        /// </summary>
        public virtual void OnDetatchedFromManager() {

        }

        public static BaseResource ReadSerialisedWithType(RBEDictionary dictionary) {
            string registryId = dictionary.GetString(nameof(FactoryId), null);
            if (string.IsNullOrEmpty(registryId))
                throw new Exception("Missing the registry ID for item");
            RBEDictionary data = dictionary.GetDictionary("Data");
            BaseResource resource = ResourceTypeFactory.Instance.NewResource(registryId);
            resource.ReadFromRBE(data);
            return resource;
        }

        public static void WriteSerialisedWithType(RBEDictionary dictionary, BaseResource item) {
            if (!(item.FactoryId is string id))
                throw new Exception("Unknown resource item type: " + item.GetType());
            dictionary.SetString(nameof(FactoryId), id);
            item.WriteToRBE(dictionary.CreateDictionary("Data"));
        }

        public static RBEDictionary WriteSerialisedWithType(BaseResource clip) {
            RBEDictionary dictionary = new RBEDictionary();
            WriteSerialisedWithType(dictionary, clip);
            return dictionary;
        }

        public virtual void ReadFromRBE(RBEDictionary data) {
            this.DisplayName = data.GetString(nameof(this.DisplayName), null);
        }

        public virtual void WriteToRBE(RBEDictionary data) {
            if (!string.IsNullOrEmpty(this.DisplayName))
                data.SetString(nameof(this.DisplayName), this.DisplayName);
        }

        /// <summary>
        /// This is invoked during the clone process. The current instance is a new cloned object.
        /// load data from the given object into the current instance
        /// </summary>
        /// <param name="clone">An object to copy data from</param>
        protected virtual void LoadDataIntoClone(BaseResource clone) {
            clone.DisplayName = this.DisplayName;
        }

        /// <summary>
        /// Invoked when this resource is about to be completely deleted. It will not have a parent object nor
        /// a manager associated with it. Dispose of unmanaged resources, unregister event handlers, etc.
        /// <para>
        /// Any errors should be logged to the application logger
        /// </para>
        /// <para>
        /// If this object is a <see cref="ResourceItem"/>, it will not be unregistered from the resource manager; that must be done manually
        /// </para>
        /// <para>
        /// If this object is a <see cref="ResourceFolder"/>, it will recursively dispose and clear the hierarchy of items
        /// </para>
        /// </summary>
        public virtual void Dispose() {
        }
    }
}