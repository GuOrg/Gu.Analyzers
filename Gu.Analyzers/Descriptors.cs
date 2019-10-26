namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class Descriptors
    {
        internal static readonly DiagnosticDescriptor GU0001NameArguments = Create(
            id: "GU0001",
            title: "Name the arguments.",
            messageFormat: "Name the arguments.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "Name the arguments of calls to methods that have more than 3 arguments and are placed on separate lines.");

        internal static readonly DiagnosticDescriptor GU0002NamedArgumentPositionMatches = Create(
            id: "GU0002",
            title: "The position of a named argument should match.",
            messageFormat: "The position of a named arguments and parameters should match.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "The position of a named argument should match.");

        internal static readonly DiagnosticDescriptor GU0003CtorParameterNamesShouldMatch = Create(
            id: "GU0003",
            title: "Name the parameter to match the assigned member.",
            messageFormat: "Name the parameter to match the assigned member.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "Name the constructor parameters to match the assigned member.");

        internal static readonly DiagnosticDescriptor GU0004AssignAllReadOnlyMembers = Descriptors.Create(
            id: "GU0004",
            title: "Assign all readonly members.",
            messageFormat: "The following readonly members are not assigned:\r\n{0}",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "Assign all readonly members.");

        internal static readonly DiagnosticDescriptor GU0005ExceptionArgumentsPositions = Descriptors.Create(
            id: "GU0005",
            title: "Use correct argument positions.",
            messageFormat: "Use correct argument positions.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Use correct position for name and message.");

        internal static readonly DiagnosticDescriptor GU0006UseNameof = Descriptors.Create(
            id: "GU0006",
            title: "Use nameof.",
            messageFormat: "Use nameof.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "Use nameof.");

        internal static readonly DiagnosticDescriptor GU0007PreferInjecting = Descriptors.Create(
            id: "GU0007",
            title: "Prefer injecting.",
            messageFormat: "Prefer injecting {0}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: false,
            description: "Prefer injecting.");

        internal static readonly DiagnosticDescriptor GU0008AvoidRelayProperties = Descriptors.Create(
            id: "GU0008",
            title: "Avoid relay properties.",
            messageFormat: "Avoid relay properties.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: false,
            description: "Avoid relay properties.");

        internal static readonly DiagnosticDescriptor GU0009UseNamedParametersForBooleans = Descriptors.Create(
            id: "GU0009",
            title: "Name the boolean parameter.",
            messageFormat: "The boolean parameter is not named.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The unnamed boolean parameters aren't obvious about their purpose. Consider naming the boolean argument for clarity.");

        internal static readonly DiagnosticDescriptor GU0010DoNotAssignSameValue = Descriptors.Create(
            id: "GU0010",
            title: "Assigning same value.",
            messageFormat: "Assigning made to same, did you mean to assign something else?",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Assigning same value does not make sense and is sign of a bug.");

        internal static readonly DiagnosticDescriptor GU0011DoNotIgnoreReturnValue = Descriptors.Create(
            id: "GU0011",
            title: "Don't ignore the return value.",
            messageFormat: "Don't ignore the return value.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't ignore the return value.");

        internal static readonly DiagnosticDescriptor GU0012NullCheckParameter = Descriptors.Create(
            id: "GU0012",
            title: "Check if parameter is null.",
            messageFormat: "Check if parameter is null.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "Check if parameter is null.");

        internal static readonly DiagnosticDescriptor GU0013TrowForCorrectParameter = Descriptors.Create(
            id: "GU0013",
            title: "Throw for correct parameter.",
            messageFormat: "Throw for correct parameter.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Throw for correct parameter.");

        internal static readonly DiagnosticDescriptor GU0014PreferParameter = Descriptors.Create(
            id: "GU0014",
            title: "Prefer using parameter.",
            messageFormat: "Prefer using parameter.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Prefer using parameter.");

        internal static readonly DiagnosticDescriptor GU0015DoNotAssignMoreThanOnce = Descriptors.Create(
            id: "GU0015",
            title: "Don't assign same more than once.",
            messageFormat: "Don't assign same more than once.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: "Don't assign same more than once.");

        internal static readonly DiagnosticDescriptor GU0016PreferLambda = Descriptors.Create(
            id: "GU0016",
            title: "Prefer lambda.",
            messageFormat: "Prefer lambda to reduce allocations.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            description: "Prefer lambda to reduce allocations.");

        internal static readonly DiagnosticDescriptor GU0017DoNotUseDiscarded = Descriptors.Create(
            id: "GU0017",
            title: "Don't use discarded.",
            messageFormat: "Don't use discarded.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't use discarded.");

        internal static readonly DiagnosticDescriptor GU0020SortProperties = Descriptors.Create(
            id: "GU0020",
            title: "Sort properties.",
            messageFormat: "Move property.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "Sort properties by StyleCop rules then by mutability.");

        internal static readonly DiagnosticDescriptor GU0021CalculatedPropertyAllocates = Descriptors.Create(
            id: "GU0021",
            title: "Calculated property allocates reference type.",
            messageFormat: "Calculated property allocates reference type.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "Calculated property allocates reference type.");

        internal static readonly DiagnosticDescriptor GU0022UseGetOnly = Descriptors.Create(
            id: "GU0022",
            title: "Use get-only.",
            messageFormat: "Use get-only.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "Use get-only.");

        internal static readonly DiagnosticDescriptor GU0023StaticMemberOrder = Descriptors.Create(
            id: "GU0023",
            title: "Static members that initialize with other static members depend on document order.",
            messageFormat: "Member '{0}' must be declared before '{1}'",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Static members that initialize with other static members depend on document order.");

        internal static readonly DiagnosticDescriptor GU0024SealTypeWithDefaultMember = Descriptors.Create(
            id: "GU0024",
            title: "Seal type with default member.",
            messageFormat: "Seal type with default member.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Seal type with default member.");

        internal static readonly DiagnosticDescriptor GU0025SealTypeWithOverridenEquality = Descriptors.Create(
            id: "GU0025",
            title: "Seal type with overridden equality.",
            messageFormat: "Seal type with overridden equality.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Seal type with overridden equality.");

        internal static readonly DiagnosticDescriptor GU0050IgnoreEventsWhenSerializing = Descriptors.Create(
            id: "GU0050",
            title: "Ignore events when serializing.",
            messageFormat: "Ignore events when serializing.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Ignore events when serializing.");

        internal static readonly DiagnosticDescriptor GU0051XmlSerializerNotCached = Descriptors.Create(
            id: "GU0051",
            title: "Cache the XmlSerializer.",
            messageFormat: "The serializer is not cached.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "This constructor loads assemblies in non-GC memory, which may cause memory leaks.");

        internal static readonly DiagnosticDescriptor GU0052ExceptionShouldBeSerializable = Descriptors.Create(
            id: "GU0052",
            title: "Mark exception with [Serializable].",
            messageFormat: "Mark exception with [Serializable].",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Mark exception with [Serializable].");

        internal static readonly DiagnosticDescriptor GU0060EnumMemberValueConflictsWithAnother = Descriptors.Create(
            id: "GU0060",
            title: "Enum member value conflict.",
            messageFormat: "Enum member value conflicts with another.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The enum member has a value shared with the other enum member, but it's not explicitly declared as its alias. To fix this, assign a enum member");

        internal static readonly DiagnosticDescriptor GU0061EnumMemberValueOutOfRange = Descriptors.Create(
            id: "GU0061",
            title: "Enum member value out of range.",
            messageFormat: "Enum member value will overflow",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The enum member value will overflow at runtime. Probably not intended. Change enum type to long (int is default)");

        internal static readonly DiagnosticDescriptor GU0070DefaultConstructedValueTypeWithNoUsefulDefault = Descriptors.Create(
            id: "GU0070",
            title: "Default-constructed value type with no useful default",
            messageFormat: "Default constructed value type was created, which is likely not what was intended.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Types declared with struct must have a default constructor, even if there is no semantically sensible default value for that type. Examples include System.Guid and System.DateTime.");

        internal static readonly DiagnosticDescriptor GU0071ForeachImplicitCast = Descriptors.Create(
            id: "GU0071",
            title: "Implicit casting done by the foreach",
            messageFormat: "Implicit cast done by the foreach",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "If an explicit type is used, the compiler inserts a cast. This was possibly useful in the pre-generic C# 1.0 era, but now it's a misfeature");

        internal static readonly DiagnosticDescriptor GU0072AllTypesShouldBeInternal = Descriptors.Create(
            id: "GU0072",
            title: "All types should be internal.",
            messageFormat: "All types should be internal.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: "All types should be internal.");

        internal static readonly DiagnosticDescriptor GU0073MemberShouldBeInternal = Descriptors.Create(
            id: "GU0073",
            title: "Member of non-public type should be internal.",
            messageFormat: "Member {0} of non-public type {1} should be internal.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Member of non-public type should be internal.");

        internal static readonly DiagnosticDescriptor GU0080TestAttributeCountMismatch = Descriptors.Create(
            id: "GU0080",
            title: "Parameter count does not match attribute.",
            messageFormat: "Parameters {0} does not match attribute {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Parameter count does not match attribute.");

        internal static readonly DiagnosticDescriptor GU0081TestCasesAttributeMismatch = Descriptors.Create(
            id: "GU0081",
            title: "TestCase does not match parameters.",
            messageFormat: "TestCase {0} does not match parameters {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "TestCase does not match parameters.");

        internal static readonly DiagnosticDescriptor GU0082IdenticalTestCase = Descriptors.Create(
            id: "GU0082",
            title: "TestCase is identical.",
            messageFormat: "TestCase is identical {0}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "TestCase is identical.");

        internal static readonly DiagnosticDescriptor GU0083TestCaseAttributeMismatchMethod = Descriptors.Create(
            id: "GU0083",
            title: "TestCase Arguments Mismatch Method Parameters",
            messageFormat: "TestCase arguments {0} does not match method parameters {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "TestCase Mismatches Method Parameters");

        internal static readonly DiagnosticDescriptor GU0090DoNotThrowNotImplementedException = Descriptors.Create(
            id: "GU0090",
            title: "Don't throw NotImplementedException.",
            messageFormat: "Don't throw NotImplementedException.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't throw NotImplementedException.");

        internal static readonly DiagnosticDescriptor GU0100WrongDocs = Descriptors.Create(
            id: "GU0100",
            title: "Wrong docs.",
            messageFormat: "Wrong docs.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Wrong docs.");

        /// <summary>
        /// Create a DiagnosticDescriptor, which provides description about a <see cref="Diagnostic" />.
        /// NOTE: For localizable <paramref name="title" />, <paramref name="description" /> and/or <paramref name="messageFormat" />,
        /// use constructor overload <see cref="DiagnosticDescriptor.#ctor(System.String,Microsoft.CodeAnalysis.LocalizableString,Microsoft.CodeAnalysis.LocalizableString,System.String,Microsoft.CodeAnalysis.DiagnosticSeverity,System.Boolean,Microsoft.CodeAnalysis.LocalizableString,System.String,System.String[])" />.
        /// </summary>
        /// <param name="id">A unique identifier for the diagnostic. For example, code analysis diagnostic ID "CA1001".</param>
        /// <param name="title">A short title describing the diagnostic. For example, for CA1001: "Types that own disposable fields should be disposable".</param>
        /// <param name="messageFormat">A format message string, which can be passed as the first argument to <see cref="String.Format(string,object[])" /> when creating the diagnostic message with this descriptor.
        /// For example, for CA1001: "Implement IDisposable on '{0}' because it creates members of the following IDisposable types: '{1}'.</param>
        /// <param name="category">The category of the diagnostic (like Design, Naming etc.). For example, for CA1001: "Microsoft.Design".</param>
        /// <param name="defaultSeverity">Default severity of the diagnostic.</param>
        /// <param name="isEnabledByDefault">True if the diagnostic is enabled by default.</param>
        /// <param name="description">An optional longer description of the diagnostic.</param>
        /// <param name="customTags">Optional custom tags for the diagnostic. See <see cref="WellKnownDiagnosticTags" /> for some well known tags.</param>
        private static DiagnosticDescriptor Create(
          string id,
          string title,
          string messageFormat,
          string category,
          DiagnosticSeverity defaultSeverity,
          bool isEnabledByDefault,
          string description = null,
          params string[] customTags)
        {
            return new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: messageFormat,
                category: category,
                defaultSeverity: defaultSeverity,
                isEnabledByDefault: isEnabledByDefault,
                description: description,
                helpLinkUri: $"https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/{id}.md",
                customTags: customTags);
        }
    }
}
