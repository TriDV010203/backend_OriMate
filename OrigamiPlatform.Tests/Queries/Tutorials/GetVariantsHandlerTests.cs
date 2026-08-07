using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetVariantsHandlerTests
{
    private readonly Mock<ITutorialVariantRepository> _mockVariantRepo;
    private readonly GetVariantsHandler _handler;

    public GetVariantsHandlerTests()
    {
        _mockVariantRepo = new Mock<ITutorialVariantRepository>();
        _handler = new GetVariantsHandler(_mockVariantRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsVariantsList()
    {
        var query = new GetVariantsQuery(Guid.NewGuid());
        
        var variants = new List<TutorialVariant>
        {
            new() 
            {
                VariantTutorialId = Guid.NewGuid(),
                DifficultyDelta = 1,
                VariantTutorial = new Tutorial 
                {
                    Title = "Variant 1",
                    Difficulty = TutorialDifficulty.Intermediate,
                    Slug = "variant-1"
                }
            }
        };

        _mockVariantRepo.Setup(r => r.GetByParentIdAsync(query.ParentTutorialId, default))
            .ReturnsAsync(variants);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Variant 1", result[0].Title);
        Assert.Equal(1, result[0].DifficultyDelta);
    }
}
