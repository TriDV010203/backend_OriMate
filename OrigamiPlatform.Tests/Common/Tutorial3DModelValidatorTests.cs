using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Common;

public class Tutorial3DModelValidatorTests
{
    [Fact]
    public void Validate_No3DMetadata_IsAllowed()
    {
        Tutorial3DModelValidator.Validate(null, null);
    }

    [Fact]
    public void Validate_ValidHttpsGlbWithQueryString_IsAllowed()
    {
        Tutorial3DModelValidator.Validate(
            "https://cdn.example.com/models/crane.GLB?v=2",
            "https://cdn.example.com/posters/crane.jpg");
    }

    [Fact]
    public void Validate_PosterWithoutModel_IsRejected()
    {
        Assert.Throws<DomainException>(() => Tutorial3DModelValidator.Validate(
            null,
            "https://cdn.example.com/posters/crane.jpg"));
    }

    [Theory]
    [InlineData("http://cdn.example.com/models/crane.glb")]
    [InlineData("/models/crane.glb")]
    [InlineData("https://cdn.example.com/models/crane.gltf")]
    [InlineData("https://cdn.example.com/models/crane.glb.exe")]
    public void Validate_UnsafeOrNonGlbModelUrl_IsRejected(string modelUrl)
    {
        Assert.Throws<DomainException>(() =>
            Tutorial3DModelValidator.Validate(modelUrl, null));
    }

    [Fact]
    public void Validate_UrlLongerThanDatabaseColumn_IsRejected()
    {
        var modelUrl = $"https://cdn.example.com/{new string('a', 500)}.glb";

        Assert.True(modelUrl.Length > Tutorial3DModelValidator.MaxUrlLength);
        Assert.Throws<DomainException>(() =>
            Tutorial3DModelValidator.Validate(modelUrl, null));
    }

    [Fact]
    public void Validate_NonHttpsPosterUrl_IsRejected()
    {
        Assert.Throws<DomainException>(() => Tutorial3DModelValidator.Validate(
            "https://cdn.example.com/models/crane.glb",
            "http://cdn.example.com/posters/crane.jpg"));
    }
}
