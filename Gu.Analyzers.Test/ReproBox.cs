namespace Gu.Analyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    [Explicit]
    public class ReproBox
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers =
            typeof(KnownSymbol).Assembly.GetTypes()
                               .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                               .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                               .ToArray();

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task Repro(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace Gu.Wpf.Reactive
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;

    using Gu.Reactive;

    /// <summary>
    /// A control for displaying conditions
    /// </summary>
    public partial class ConditionControl : Control
    {
#pragma warning disable SA1202 // Elements must be ordered by access
#pragma warning disable SA1600 // Elements must be documented
#pragma warning disable 1591
        private static readonly IEnumerable<ICondition> Empty = new ICondition[0];

        public static readonly DependencyProperty ConditionProperty = DependencyProperty.Register(
            nameof(Condition),
            typeof(ICondition),
            typeof(ConditionControl),
            new PropertyMetadata(default(ICondition), OnConditionChanged));

        private static readonly DependencyPropertyKey RootPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Root),
            typeof(IEnumerable<ICondition>),
            typeof(ConditionControl),
            new PropertyMetadata(Empty));

        public static readonly DependencyProperty RootProperty = RootPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey FlattenedPrerequisitesPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FlattenedPrerequisites),
            typeof(IEnumerable<ICondition>),
            typeof(ConditionControl),
            new PropertyMetadata(Empty));

        private static readonly DependencyPropertyKey IsInSyncPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsInSync),
            typeof(bool),
            typeof(ConditionControl),
            new PropertyMetadata(true));

        public static readonly DependencyProperty IsInSyncProperty = IsInSyncPropertyKey.DependencyProperty;

        public static readonly DependencyProperty FlattenedPrerequisitesProperty = FlattenedPrerequisitesPropertyKey.DependencyProperty;

#pragma warning restore SA1202 // Elements must be ordered by access
#pragma warning restore 1591
#pragma warning restore SA1600 // Elements must be documented

        static ConditionControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConditionControl), new FrameworkPropertyMetadata(typeof(ConditionControl)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref=""ConditionControl""/> class.
        /// </summary>
        public ConditionControl()
        {
            this.IsVisibleChanged += (_, __) => this.OnIsVisibleChanged();
        }

        /// <summary>
        /// The condition exposed as an enumerable with one element for binding the root of <see cref=""TreeView""/>.
        /// </summary>
        public IEnumerable<ICondition> Root
        {
            get { return (IEnumerable<ICondition>)this.GetValue(RootProperty); }
            protected set { this.SetValue(RootPropertyKey, value); }
        }

        /// <summary>
        /// A flat list of all conditions
        /// </summary>
        public IEnumerable<ICondition> FlattenedPrerequisites
        {
            get { return (IEnumerable<ICondition>)this.GetValue(FlattenedPrerequisitesProperty); }
            protected set { this.SetValue(FlattenedPrerequisitesPropertyKey, value); }
        }

        /// <summary>
        /// True if all detected changes of ICondition.IsSatisfied have been notified.
        /// </summary>
        public bool IsInSync
        {
            get { return (bool)this.GetValue(IsInSyncProperty); }
            protected set { this.SetValue(IsInSyncPropertyKey, value); }
        }

        /// <summary>
        /// The condition.
        /// </summary>
        public ICondition Condition
        {
            get { return (ICondition)this.GetValue(ConditionProperty); }
            set { this.SetValue(ConditionProperty, value); }
        }

        /// <summary>
        /// Called when the <see cref=""Condition""/> changes.
        /// </summary>
        // ReSharper disable once UnusedParameter.Global
        protected virtual void OnConditionChanged(ICondition oldCondition, ICondition newCondition)
        {
            if (newCondition == null)
            {
                this.Root = Empty;
                this.FlattenedPrerequisites = Empty;
                return;
            }

            this.Root = new[] { newCondition };
            this.FlattenedPrerequisites = FlattenPrerequisites(newCondition);
            this.IsInSync = this.Condition.IsInSync();
        }

        private static void OnConditionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var conditionControl = (ConditionControl)o;
            conditionControl.OnConditionChanged((ICondition)e.OldValue, (ICondition)e.NewValue);
        }

        private static IReadOnlyList<ICondition> FlattenPrerequisites(ICondition condition, List<ICondition> list = null)
        {
            if (list == null)
            {
                list = new List<ICondition>();
            }

            if (list.Contains(condition))
            {
                return list; // Break recursion
            }

            list.Add(condition);
            foreach (var pre in condition.Prerequisites)
            {
                FlattenPrerequisites(pre, list);
            }

            return list;
        }

        private void OnIsVisibleChanged()
        {
            if (this.Condition != null &&
                this.Visibility == Visibility.Visible &&
                this.IsInSync)
            {
                this.IsInSync = this.Condition.IsInSync();
            }
        }
    }
}";

            Console.WriteLine(analyzer);
            var analyzers = ImmutableArray.Create(analyzer);
            await DiagnosticVerifier.GetSortedDiagnosticsFromDocumentsAsync(
                          analyzers,
                          CodeFactory.GetDocuments(
                              new[] { testCode },
                              analyzers,
                              Enumerable.Empty<string>()),
                          CancellationToken.None)
                      .ConfigureAwait(false);
        }
    }
}