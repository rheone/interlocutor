namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using System.Reflection;

/// <summary>
/// Covers <see cref="AssemblyTypeScanner"/> - the single call site every scan path in this package goes
/// through, so a <see cref="ReflectionTypeLoadException"/> from one unloadable type never aborts the whole
/// scan. <see cref="ReflectionTypeLoadException"/>'s own public constructor accepts the exact
/// <c>Types</c>/<c>LoaderExceptions</c> shape <see cref="Assembly.GetTypes"/> itself produces when it throws,
/// so <see cref="AssemblyTypeScanner.ExtractLoadedTypes"/> - the catch branch's own logic - is exercised
/// directly against a manually-constructed exception rather than requiring a genuinely unloadable assembly
/// on disk.
/// </summary>
public class AssemblyTypeScannerTests
{
    [Fact]
    public void GetLoadableTypes_WithANormallyLoadableAssembly_ReturnsEveryTypeAndRecordsNoFailures()
    {
        // Arrange
        var failures = new List<TypeLoadFailure>();

        // Act
        var types = AssemblyTypeScanner.GetLoadableTypes(typeof(AssemblyTypeScannerTests).Assembly, failures).ToList();

        // Assert
        Assert.Contains(typeof(AssemblyTypeScannerTests), types);
        Assert.Empty(failures);
    }

    [Fact]
    public void GetLoadableTypes_WithNoFailuresCollection_StillReturnsEveryType()
    {
        // Act
        var types = AssemblyTypeScanner.GetLoadableTypes(typeof(AssemblyTypeScannerTests).Assembly, failures: null).ToList();

        // Assert
        Assert.Contains(typeof(AssemblyTypeScannerTests), types);
    }

    [Fact]
    public void ExtractLoadedTypes_ReturnsTheTypesThatLoaded_ContinuingPastTheOnesThatDidNot()
    {
        // Mirrors what Assembly.GetTypes() itself hands back in ReflectionTypeLoadException.Types when one
        // assembly has a mix of loadable and unloadable types: a real Type entry for each success, null for
        // each failure - ticket 57's "continue scanning past the types that did load successfully".
        // Arrange
        var loaded = typeof(AssemblyTypeScannerTests);
        var ex = new ReflectionTypeLoadException(
            [loaded, null],
            [null, new TypeLoadException("Could not load type 'Missing'.")]
        );

        // Act
        var types = AssemblyTypeScanner
            .ExtractLoadedTypes(typeof(AssemblyTypeScannerTests).Assembly, ex, failures: null)
            .ToList();

        // Assert
        Assert.Equal([loaded], types);
    }

    [Fact]
    public void ExtractLoadedTypes_RecordsOneFailurePerUnloadedType_WithTheAssemblyNameAndLoaderMessage()
    {
        // Arrange
        var assembly = typeof(AssemblyTypeScannerTests).Assembly;
        var ex = new ReflectionTypeLoadException(
            [null, null],
            [new TypeLoadException("Could not load type 'First'."), new TypeLoadException("Could not load type 'Second'.")]
        );
        var failures = new List<TypeLoadFailure>();

        // Act
        AssemblyTypeScanner.ExtractLoadedTypes(assembly, ex, failures).ToList();

        // Assert
        Assert.Equal(2, failures.Count);
        Assert.All(failures, f => Assert.Equal(assembly.GetName().Name, f.AssemblyName));
        Assert.Contains(failures, f => f.Detail.Contains("First"));
        Assert.Contains(failures, f => f.Detail.Contains("Second"));
    }

    [Fact]
    public void ExtractLoadedTypes_WithNoFailuresCollection_StillReturnsTheTypesThatLoaded_WithoutThrowing()
    {
        // Arrange
        var loaded = typeof(AssemblyTypeScannerTests);
        var ex = new ReflectionTypeLoadException(
            [loaded, null],
            [null, new TypeLoadException("Could not load type 'Missing'.")]
        );

        // Act
        var types = AssemblyTypeScanner
            .ExtractLoadedTypes(typeof(AssemblyTypeScannerTests).Assembly, ex, failures: null)
            .ToList();

        // Assert
        Assert.Equal([loaded], types);
    }

    [Fact]
    public void ExtractLoadedTypes_WithNoLoaderExceptionForAFailedType_FallsBackToAGenericMessage()
    {
        // ReflectionTypeLoadException.LoaderExceptions can itself be shorter than Types (or hold a null
        // entry) - the fallback keeps every failed type discoverable even without loader detail.
        // Arrange
        var assembly = typeof(AssemblyTypeScannerTests).Assembly;
        var ex = new ReflectionTypeLoadException([null], []);
        var failures = new List<TypeLoadFailure>();

        // Act
        AssemblyTypeScanner.ExtractLoadedTypes(assembly, ex, failures).ToList();

        // Assert
        var failure = Assert.Single(failures);
        Assert.Equal("Unknown type load failure.", failure.Detail);
    }
}
