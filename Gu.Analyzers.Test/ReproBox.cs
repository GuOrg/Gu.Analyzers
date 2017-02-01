namespace Gu.Analyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    public class ReproBox : DiagnosticVerifier
    {
        private static readonly IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers =
            typeof(Gu.Analyzers.KnownSymbol).Assembly.GetTypes()
                                            .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                                            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                                            .ToArray();

        private readonly List<DiagnosticAnalyzer> analyzers = new List<DiagnosticAnalyzer>();

        public override void IdMatches()
        {
            Assert.Pass();
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void Repro(DiagnosticAnalyzer analyzer)
        {
            var iDirtyTrackerCode = @"namespace Gu.State
{
    using System;
    using System.ComponentModel;

    /// <summary>A tracker that tracks the difference between tow instances.</summary>
    public interface IDirtyTracker : INotifyPropertyChanged, IDisposable
    {
        /// <summary>Gets the settings specifying how tracking and equality is performed.</summary>
        PropertiesSettings Settings { get; }

        /// <summary>Gets a value indicating whether there is a difference between x and y.</summary>
        bool IsDirty { get; }

        /// <summary>Gets the difference between x and y. This is a mutable value.</summary>
        ValueDiff Diff { get; }
    }
}";
            var dirtyTrackerCode = @"namespace Gu.State
{
    using System.ComponentModel;

    internal sealed class DirtyTracker : IDirtyTracker
    {
        private static readonly PropertyChangedEventArgs DiffPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Diff));
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));

        private readonly IRefCounted<DirtyTrackerNode> node;
        private bool disposed;

        internal DirtyTracker(INotifyPropertyChanged x, INotifyPropertyChanged y, PropertiesSettings settings)
        {
            this.Settings = settings;
            this.node = DirtyTrackerNode.GetOrCreate(x, y, settings, true);
            this.node.Value.PropertyChanged += this.OnNodeChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public PropertiesSettings Settings { get; }

        public bool IsDirty => this.node.Value.IsDirty;

        public ValueDiff Diff => this.node.Value.Diff;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.node.Value.PropertyChanged -= this.OnNodeChanged;
            this.node.Dispose();
        }

        private void OnNodeChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DirtyTrackerNode.Diff))
            {
                this.PropertyChanged?.Invoke(this, DiffPropertyChangedEventArgs);
            }
            else if (e.PropertyName == nameof(DirtyTrackerNode.IsDirty))
            {
                this.PropertyChanged?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
            else
            {
                throw Throw.ShouldNeverGetHereException($""Expected property name {nameof(DirtyTrackerNode.Diff)} || {nameof(DirtyTrackerNode.IsDirty)}"");
            }
        }
    }
}";

            var dirtyTrackerNodeCode = @"
namespace Gu.State
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    internal sealed class DirtyTrackerNode : IDirtyTracker, IInitialize<DirtyTrackerNode>, ITrackerNode<DirtyTrackerNode>
    {
        private static readonly PropertyChangedEventArgs DiffPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Diff));
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));

        private readonly IRefCounted<ReferencePair> refCountedPair;
        private readonly IRefCounted<RootChanges> xNode;
        private readonly IRefCounted<RootChanges> yNode;
        private readonly IBorrowed<ChildNodes<DirtyTrackerNode>> children;
        private readonly IRefCounted<DiffBuilder> refcountedDiffBuilder;

        private bool isDirty;

        /// <summary>
        /// Initializes a new instance of the <see cref=""DirtyTrackerNode""/> class.
        /// A call to Initialize is needed after the ctor due to that we need to fetch child nodes and the graph can contain self
        /// </summary>
        private DirtyTrackerNode(IRefCounted<ReferencePair> refCountedPair, PropertiesSettings settings, bool isRoot)
        {
            this.refCountedPair = refCountedPair;
            var x = refCountedPair.Value.X;
            var y = refCountedPair.Value.Y;
            this.children = ChildNodes<DirtyTrackerNode>.Borrow();
            this.xNode = RootChanges.GetOrCreate(x, settings, isRoot);
            this.yNode = RootChanges.GetOrCreate(y, settings, isRoot);
            this.xNode.Value.PropertyChange += this.OnTrackedPropertyChange;
            this.yNode.Value.PropertyChange += this.OnTrackedPropertyChange;

            this.IsTrackingCollectionItems = Is.Enumerable(x, y) &&
                                 !settings.IsImmutable(x.GetType().GetItemType()) &&
                                 !settings.IsImmutable(y.GetType().GetItemType());

            if (Is.NotifyingCollections(x, y))
            {
                this.xNode.Value.Add += this.OnTrackedAdd;
                this.xNode.Value.Remove += this.OnTrackedRemove;
                this.xNode.Value.Replace += this.OnTrackedReplace;
                this.xNode.Value.Move += this.OnTrackedMove;
                this.xNode.Value.Reset += this.OnTrackedReset;

                this.yNode.Value.Add += this.OnTrackedAdd;
                this.yNode.Value.Remove += this.OnTrackedRemove;
                this.yNode.Value.Replace += this.OnTrackedReplace;
                this.yNode.Value.Move += this.OnTrackedMove;
                this.yNode.Value.Reset += this.OnTrackedReset;
            }

            var builder = DiffBuilder.GetOrCreate(x, y, settings);
            builder.Value.UpdateDiffs(x, y, settings);
            builder.Value.Refresh();
            this.refcountedDiffBuilder = builder;
            this.isDirty = !this.Builder.IsEmpty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<TrackerChangedEventArgs<DirtyTrackerNode>> Changed;

        public PropertiesSettings Settings => this.xNode.Value.Settings;

        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            private set
            {
                if (value == this.isDirty)
                {
                    return;
                }

                this.isDirty = value;
                this.PropertyChanged?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
        }

        public ValueDiff Diff => this.Builder?.CreateValueDiffOrNull();

        internal object X => this.xNode.Value.Source;

        internal object Y => this.yNode.Value.Source;

        private bool IsTrackingCollectionItems { get; }

        private DiffBuilder Builder => this.refcountedDiffBuilder?.Value;

        private IList XList => (IList)this.X;

        private IList YList => (IList)this.Y;

        private IReadOnlyCollection<PropertyInfo> TrackProperties => this.xNode.Value.TrackProperties;

        private ChildNodes<DirtyTrackerNode> Children => this.children.Value;

        public void Dispose()
        {
            this.xNode.Value.PropertyChange -= this.OnTrackedPropertyChange;
            this.xNode.Value.Add -= this.OnTrackedAdd;
            this.xNode.Value.Remove -= this.OnTrackedRemove;
            this.xNode.Value.Remove -= this.OnTrackedRemove;
            this.xNode.Value.Move -= this.OnTrackedMove;
            this.xNode.Value.Reset -= this.OnTrackedReset;
            this.xNode.Dispose();

            this.yNode.Value.PropertyChange -= this.OnTrackedPropertyChange;
            this.yNode.Value.Add -= this.OnTrackedAdd;
            this.yNode.Value.Remove -= this.OnTrackedRemove;
            this.yNode.Value.Remove -= this.OnTrackedRemove;
            this.yNode.Value.Move -= this.OnTrackedMove;
            this.yNode.Value.Reset -= this.OnTrackedReset;
            this.yNode.Dispose();

            this.children.Dispose();
            this.refCountedPair.Dispose();
            this.refcountedDiffBuilder.Dispose();
        }

        // Initialize is needed here as we can't get recursive trackers in the ctor
        // Call the ctor like new DirtyTrackerNode(pair, settings).Initialize()
        DirtyTrackerNode IInitialize<DirtyTrackerNode>.Initialize()
        {
            foreach (var propertyInfo in this.TrackProperties)
            {
                this.UpdatePropertyChildNode(propertyInfo);
            }

            if (this.IsTrackingCollectionItems)
            {
                for (var i = 0; i < Math.Max(this.XList.Count, this.YList.Count); i++)
                {
                    this.UpdateIndexChildNode(i);
                }
            }

            return this;
        }

        internal static IRefCounted<DirtyTrackerNode> GetOrCreate(object x, object y, PropertiesSettings settings, bool isRoot)
        {
            Debug.Assert(x != null, ""Cannot track null"");
            Debug.Assert(x is INotifyPropertyChanged || x is INotifyCollectionChanged, ""Must notify"");
            Debug.Assert(y != null, ""Cannot track null"");
            Debug.Assert(y is INotifyPropertyChanged || y is INotifyCollectionChanged, ""Must notify"");
            return TrackerCache.GetOrAdd(
                x,
                y,
                settings,
                pair => new DirtyTrackerNode(pair, settings, isRoot));
        }

        internal IEnumerable<DirtyTrackerNode> AllChildNodes()
        {
            using (var borrow = ReferenceSetPool<DirtyTrackerNode>.Borrow())
            {
                return this.AllChildNodes(borrow.Value);
            }
        }

        private static bool IsTrackablePair(object x, object y, PropertiesSettings settings)
        {
            if (IsNullOrMissing(x) || IsNullOrMissing(y))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return !settings.IsImmutable(x.GetType()) && !settings.IsImmutable(y.GetType());
        }

        private static bool IsNullOrMissing(object x)
        {
            return x == null || x == PaddedPairs.MissingItem;
        }

        private IEnumerable<DirtyTrackerNode> AllChildNodes(HashSet<DirtyTrackerNode> @checked)
        {
            foreach (var node in this.Children.AllChildren())
            {
                if (!@checked.Add(node))
                {
                    continue;
                }

                yield return node;
                foreach (var child in node.AllChildNodes(@checked))
                {
                    yield return child;
                }
            }
        }

        private void OnTrackedPropertyChange(object sender, PropertyChangeEventArgs e)
        {
            this.UpdatePropertyChildNode(e.PropertyInfo);
            //// we create the builder after subscribing so no guarantee that we have a builder if an event fires before the ctor is finished.
            if (this.Builder == null ||
                this.Settings.IsIgnoringProperty(e.PropertyInfo))
            {
                return;
            }

            this.Builder.UpdateMemberDiff(this.X, this.Y, e.PropertyInfo, this.Settings);
            this.TryRefreshAndNotify(e);
        }

        private void UpdatePropertyChildNode(PropertyInfo propertyInfo)
        {
            if (this.Settings.IsIgnoringProperty(propertyInfo))
            {
                return;
            }

            if (this.TrackProperties.Contains(propertyInfo) &&
               (this.Settings.ReferenceHandling == ReferenceHandling.Structural))
            {
                var getter = this.Settings.GetOrCreateGetterAndSetter(propertyInfo);
                var xValue = getter.GetValue(this.X);
                var yValue = getter.GetValue(this.Y);
                IRefCounted<DirtyTrackerNode> node;
                if (this.TrCreateChild(xValue, yValue, out node))
                {
                    using (node)
                    {
                        var childNode = ChildNodes<DirtyTrackerNode>.CreateChildNode(this, node.Value, propertyInfo);
                        childNode.Changed += this.OnChildNodeChanged;
                        this.Children.SetValue(propertyInfo, childNode.UnsubscribeAndDispose(x => x.Changed -= this.OnChildNodeChanged));
                    }
                }
                else
                {
                    this.Children.Remove(propertyInfo);
                }
            }
        }

        private void OnTrackedAdd(object sender, AddEventArgs e)
        {
            for (int i = e.Index; i < Math.Max(this.XList.Count, this.YList.Count); i++)
            {
                this.UpdateIndexChildNode(i);
                this.UpdateIndexDiff(i);
            }

            this.TryRefreshAndNotify(e);
        }

        private void OnTrackedRemove(object sender, RemoveEventArgs e)
        {
            for (int i = e.Index; i <= Math.Max(this.XList.Count, this.YList.Count); i++)
            {
                this.UpdateIndexChildNode(i);
                this.UpdateIndexDiff(i);
            }

            this.TryRefreshAndNotify(e);
        }

        private void OnTrackedReplace(object sender, ReplaceEventArgs e)
        {
            this.UpdateIndexChildNode(e.Index);
            this.UpdateIndexDiff(e.Index);
            this.TryRefreshAndNotify(e);
        }

        private void OnTrackedMove(object sender, MoveEventArgs e)
        {
            for (int i = Math.Min(e.FromIndex, e.ToIndex); i <= Math.Max(e.FromIndex, e.ToIndex); i++)
            {
                this.UpdateIndexChildNode(i);
                this.UpdateIndexDiff(i);
            }

            this.TryRefreshAndNotify(e);
        }

        private void OnTrackedReset(object sender, ResetEventArgs e)
        {
            this.Builder?.ClearIndexDiffs();
            using (var borrowed = ListPool<IUnsubscriber<IChildNode<DirtyTrackerNode>>>.Borrow())
            {
                var max = Math.Max(this.XList.Count, this.YList.Count);
                for (var i = 0; i < max; i++)
                {
                    this.UpdateIndexDiff(i);

                    var childNode = this.CreateChildNode(i);
                    if (childNode != null)
                    {
                        borrowed.Value.Add(childNode);
                    }
                }

                this.Children.Reset(borrowed.Value);
            }

            this.TryRefreshAndNotify(e);
        }

        private void UpdateIndexChildNode(int index)
        {
            var childNode = this.CreateChildNode(index);
            if (childNode != null)
            {
                this.Children.Replace(index, childNode);
            }
            else
            {
                this.Children.Remove(index);
            }
        }

        private IUnsubscriber<IChildNode<DirtyTrackerNode>> CreateChildNode(int index)
        {
            if (!this.IsTrackingCollectionItems)
            {
                return null;
            }

            var xValue = this.XList.ElementAtOrMissing(index);
            var yValue = this.YList.ElementAtOrMissing(index);

            IRefCounted<DirtyTrackerNode> node;
            if (this.TrCreateChild(xValue, yValue, out node))
            {
                using (node)
                {
                    var childNode = ChildNodes<DirtyTrackerNode>.CreateChildNode(this, node.Value, index);
                    childNode.Changed += this.OnChildNodeChanged;
                    return childNode.UnsubscribeAndDispose(x => x.Changed -= this.OnChildNodeChanged);
                }
            }

            return null;
        }

        private void UpdateIndexDiff(int index)
        {
            // we create the builder after subscribing so no guarantee that we have a builder if an event fires before the ctor is finished.
            if (this.Builder == null)
            {
                return;
            }

            var xValue = this.XList.ElementAtOrMissing(index);
            var yValue = this.YList.ElementAtOrMissing(index);
            this.Builder.UpdateCollectionItemDiff(xValue, yValue, index, this.Settings);
        }

        private bool TrCreateChild(object xValue, object yValue, out IRefCounted<DirtyTrackerNode> childNode)
        {
            if (!IsTrackablePair(xValue, yValue, this.Settings) ||
                this.Settings.ReferenceHandling != ReferenceHandling.Structural)
            {
                childNode = null;
                return false;
            }

            childNode = GetOrCreate(xValue, yValue, this.Settings, false);
            return true;
        }

        // ReSharper disable once UnusedParameter.Local
        private void OnChildNodeChanged(object _, TrackerChangedEventArgs<DirtyTrackerNode> e)
        {
            if (this.Builder == null)
            {
                return;
            }

            if (e.Previous?.Contains(this) == true)
            {
                return;
            }

            var propertyGraphChangedEventArgs = e as PropertyGraphChangedEventArgs<DirtyTrackerNode>;
            if (propertyGraphChangedEventArgs != null)
            {
                this.OnChildNodeChanged(propertyGraphChangedEventArgs);
                return;
            }

            var itemGraphChangedEventArgs = e as ItemGraphChangedEventArgs<DirtyTrackerNode>;
            if (itemGraphChangedEventArgs != null)
            {
                this.OnChildNodeChanged(itemGraphChangedEventArgs);
                return;
            }

            throw Throw.ExpectedParameterOfTypes<PropertyGraphChangedEventArgs<DirtyTrackerNode>, ItemGraphChangedEventArgs<DirtyTrackerNode>>(""OnChildNodeChanged failed."");
        }

        private void OnChildNodeChanged(PropertyGraphChangedEventArgs<DirtyTrackerNode> e)
        {
            if (this.Settings.IsIgnoringProperty(e.Property))
            {
                return;
            }

            this.Builder.UpdateMemberDiff(this.X, this.Y, e.Property, this.Settings);
            this.Builder.Refresh();
            this.PropertyChanged?.Invoke(this, DiffPropertyChangedEventArgs);
            this.IsDirty = !this.Builder.IsEmpty;
            this.Changed?.Invoke(this, e.With(this, e.Property));
        }

        private void OnChildNodeChanged(ItemGraphChangedEventArgs<DirtyTrackerNode> e)
        {
            this.Builder.UpdateCollectionItemDiff(this.XList.ElementAtOrMissing(e.Index), this.YList.ElementAtOrMissing(e.Index), e.Index, this.Settings);
            this.Builder.Refresh();
            this.PropertyChanged?.Invoke(this, DiffPropertyChangedEventArgs);
            this.IsDirty = !this.Builder.IsEmpty;
            this.Changed?.Invoke(this, e.With(this, e.Index));
        }

        private void TryRefreshAndNotify<T>(T e)
            where T : IRootChangeEventArgs
        {
            if (this.Builder?.TryRefresh() == true)
            {
                this.PropertyChanged?.Invoke(this, DiffPropertyChangedEventArgs);
                this.IsDirty = !this.Builder.IsEmpty;
                this.Changed?.Invoke(this, RootChangeEventArgs.Create(this, e));
            }
        }
    }
}";

            Console.WriteLine(analyzer);
            this.analyzers.Clear();
            this.analyzers.Add(analyzer);
            Assert.ThrowsAsync<AssertionException>(() => this.VerifyCSharpDiagnosticAsync(new[] { iDirtyTrackerCode, dirtyTrackerCode, dirtyTrackerNodeCode }, EmptyDiagnosticResults));
        }

        internal override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            return this.analyzers;
        }
    }
}