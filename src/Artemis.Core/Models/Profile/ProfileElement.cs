using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SkiaSharp;
using Stylet;

namespace Artemis.Core.Models.Profile
{
    public abstract class ProfileElement : PropertyChangedBase, IDisposable
    {
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool _disposed;
        private bool _enabled;
        private Guid _entityId;
        private string _name;
        private int _order;
        private ProfileElement _parent;
        private Profile _profile;
        protected List<ProfileElement> ChildrenList;

        protected ProfileElement()
        {
            ChildrenList = new List<ProfileElement>();
        }

        public Guid EntityId
        {
            get => _entityId;
            internal set => SetAndNotify(ref _entityId, value);
        }

        public Profile Profile
        {
            get => _profile;
            internal set => SetAndNotify(ref _profile, value);
        }

        public ProfileElement Parent
        {
            get => _parent;
            internal set => SetAndNotify(ref _parent, value);
        }

        /// <summary>
        ///     The element's children
        /// </summary>
        public ReadOnlyCollection<ProfileElement> Children => ChildrenList.AsReadOnly();

        /// <summary>
        ///     The order in which this element appears in the update loop and editor
        /// </summary>
        public int Order
        {
            get => _order;
            internal set => SetAndNotify(ref _order, value);
        }

        /// <summary>
        ///     The name which appears in the editor
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }

        /// <summary>
        ///     Gets or sets the enabled state, if not enabled the element is skipped in render and update
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => SetAndNotify(ref _enabled, value);
        }

        /// <summary>
        ///     Updates the element
        /// </summary>
        /// <param name="deltaTime"></param>
        public abstract void Update(double deltaTime);

        /// <summary>
        ///     Renders the element
        /// </summary>
        public abstract void Render(double deltaTime, SKCanvas canvas, SKImageInfo canvasInfo);

        public List<Folder> GetAllFolders()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            var folders = new List<Folder>();
            foreach (var childFolder in Children.Where(c => c is Folder).Cast<Folder>())
            {
                // Add all folders in this element
                folders.Add(childFolder);
                // Add all folders in folders inside this element
                folders.AddRange(childFolder.GetAllFolders());
            }

            return folders;
        }

        public List<Layer> GetAllLayers()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            var layers = new List<Layer>();

            // Add all layers in this element
            layers.AddRange(Children.Where(c => c is Layer).Cast<Layer>());

            // Add all layers in folders inside this element
            foreach (var childFolder in Children.Where(c => c is Folder).Cast<Folder>())
                layers.AddRange(childFolder.GetAllLayers());

            return layers;
        }

        /// <summary>
        ///     Adds a profile element to the <see cref="Children" /> collection, optionally at the given position (1-based)
        /// </summary>
        /// <param name="child">The profile element to add</param>
        /// <param name="order">The order where to place the child (1-based), defaults to the end of the collection</param>
        public virtual void AddChild(ProfileElement child, int? order = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            lock (ChildrenList)
            {
                // Add to the end of the list
                if (order == null)
                {
                    ChildrenList.Add(child);
                    child.Order = ChildrenList.Count;
                }
                // Shift everything after the given order
                else
                {
                    foreach (var profileElement in ChildrenList.Where(c => c.Order >= order).ToList())
                        profileElement.Order++;

                    int targetIndex;
                    if (order == 0)
                        targetIndex = 0;
                    else if (order > ChildrenList.Count)
                        targetIndex = ChildrenList.Count;
                    else
                        targetIndex = ChildrenList.FindIndex(c => c.Order == order + 1);

                    ChildrenList.Insert(targetIndex, child);
                    child.Order = order.Value;
                }

                child.Parent = this;
            }

            OnChildAdded();
        }

        /// <summary>
        ///     Removes a profile element from the <see cref="Children" /> collection
        /// </summary>
        /// <param name="child">The profile element to remove</param>
        public virtual void RemoveChild(ProfileElement child)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            lock (ChildrenList)
            {
                ChildrenList.Remove(child);

                // Shift everything after the given order
                foreach (var profileElement in ChildrenList.Where(c => c.Order > child.Order).ToList())
                    profileElement.Order--;

                child.Parent = null;
            }

            OnChildRemoved();
        }

        public override string ToString()
        {
            return $"{nameof(EntityId)}: {EntityId}, {nameof(Order)}: {Order}, {nameof(Name)}: {Name}";
        }

        /// <summary>
        ///     Applies the profile element's properties to the underlying storage entity
        /// </summary>
        internal abstract void ApplyToEntity();

        #region Events

        public event EventHandler ChildAdded;
        public event EventHandler ChildRemoved;

        protected virtual void OnChildAdded()
        {
            ChildAdded?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnChildRemoved()
        {
            ChildRemoved?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}