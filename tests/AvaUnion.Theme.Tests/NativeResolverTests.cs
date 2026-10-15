using AvaUnion.Engine;
using AvaUnion.Engine.Native;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class NativeResolverTests
{
    [Theory]
    [InlineData("6.8.0", "6.8.0", true)]
    [InlineData("6.8.0", "6.7.91", true)]
    [InlineData("6.8.0", "6.7.90", true)]
    [InlineData("6.7.91", "6.8.0", true)]
    [InlineData("6.8.0", "6.8.3", true)]
    [InlineData("6.8.0", "6.7.5", false)]
    [InlineData("6.8.0", "6.7.9", false)]
    [InlineData("6.7.5", "6.7.0", true)]
    [InlineData("6.8.0", "6.9.0", false)]
    [InlineData("6.8.0", "7.8.0", false)]
    [InlineData("6.8.0", "6.8.90", false)]
    [InlineData("6.8.0", "6.8.80", false)]
    [InlineData("6.8.80", "6.8.80", true)]
    [InlineData("6.8.80", "6.8.81", false)]
    [InlineData("6.8.0", "garbage", false)]
    public void Shims_Load_On_Union_Versions_With_The_Same_Property_Schema(string shim, string system, bool compatible)
    {
        Assert.Equal(compatible, NativeLibraryResolver.Compatible(shim, system));
    }

    [Fact]
    public void The_Pinned_Union_Has_The_Generated_Schema()
    {
        Assert.Equal(UnionLeafInfo.Schema, NativeLibraryResolver.SchemaOf(TestData.UnionVersion));
    }
}
