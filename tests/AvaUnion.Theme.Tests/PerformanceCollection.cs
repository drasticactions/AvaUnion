using Xunit;

namespace AvaUnion.Theme.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PerformanceCollection
{
    public const string Name = "Performance";
}
