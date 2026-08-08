using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Subscriptions;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Tests.Commands.Subscriptions;

public class ConfigureVipTierHandlerTests
{
    private readonly Mock<ICreatorVipSettingsRepository> _mockSettings;
    private readonly ConfigureVipTierHandler _handler;

    public ConfigureVipTierHandlerTests()
    {
        _mockSettings = new Mock<ICreatorVipSettingsRepository>();
        _handler = new ConfigureVipTierHandler(_mockSettings.Object);
    }

    [Fact]
    public async Task HandleAsync_SettingsDoNotExist_CreatesSettings()
    {
        var command = new ConfigureVipTierCommand(Guid.NewGuid(), true);
        _mockSettings.Setup(s => s.GetByCreatorIdAsync(command.CreatorId, default)).ReturnsAsync((CreatorVipSettings?)null);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsActive);
        Assert.Equal(VipConstants.FixedPriceVnd, result.Price);
        _mockSettings.Verify(s => s.AddAsync(It.Is<CreatorVipSettings>(x => x.CreatorId == command.CreatorId && x.IsActive == true && x.Price == VipConstants.FixedPriceVnd), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SettingsExist_UpdatesSettings()
    {
        var command = new ConfigureVipTierCommand(Guid.NewGuid(), false);
        var existingSettings = new CreatorVipSettings { Id = Guid.NewGuid(), CreatorId = command.CreatorId, IsActive = true, Price = 100000 };
        _mockSettings.Setup(s => s.GetByCreatorIdAsync(command.CreatorId, default)).ReturnsAsync(existingSettings);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsActive);
        Assert.Equal(VipConstants.FixedPriceVnd, result.Price);
        _mockSettings.Verify(s => s.UpdateAsync(It.Is<CreatorVipSettings>(x => x.Id == existingSettings.Id && x.IsActive == false && x.Price == VipConstants.FixedPriceVnd), default), Times.Once);
    }
}
