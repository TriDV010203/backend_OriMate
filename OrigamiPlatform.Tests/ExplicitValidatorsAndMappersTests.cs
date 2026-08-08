using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Commands.Journals;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using OrigamiPlatform.Application.Commands.LearningPaths;
using Moq;
using OrigamiPlatform.Application.Interfaces;
using System.Threading;

namespace OrigamiPlatform.Tests.Queries;

public class ExplicitValidatorsAndMappersTests
{
    [Fact]
    public void SubscriptionMapping_ToAdminDto_MapsCorrectly()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "user@test.com", Profile = new UserProfile { DisplayName = "User1" } };
        var creator = new User { Id = Guid.NewGuid(), Email = "creator@test.com", Profile = new UserProfile { DisplayName = "Creator1" } };

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            CreatorId = creator.Id,
            Creator = creator,
            TransactionType = TransactionType.VipSubscription,
            Amount = 100,
            PlatformFeeAmount = 10,
            CreatorNetAmount = 90,
            Status = TransactionStatus.Confirmed,
            ReferenceCode = "REF123",
            CreatedAt = DateTime.UtcNow
        };

        var adminDto = transaction.ToAdminDto();
        Assert.Equal("User1", adminDto.SubscriberDisplayName);
        Assert.Equal("Creator1", adminDto.CreatorDisplayName);
        Assert.Equal("REF123", adminDto.ReferenceCode);
    }

    [Fact]
    public void SubscriptionMapping_ToMyDto_MapsCorrectly()
    {
        var creator = new User { Id = Guid.NewGuid(), Email = "creator@test.com", Profile = new UserProfile { DisplayName = "Creator1" } };
        var sub = new VipSubscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = Guid.NewGuid(),
            CreatorId = creator.Id,
            Creator = creator,
            TransactionId = Guid.NewGuid(),
            Transaction = new Transaction { Amount = 100 },
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(29),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var myDto = sub.ToMyDto();
        Assert.Equal("Creator1", myDto.CreatorDisplayName);
        Assert.Equal(100, myDto.Price);
        Assert.Equal(SubscriptionStatus.Active.ToString(), myDto.Status);
    }

    [Fact]
    public void JournalRequestValidator_Validate_ValidInput_ReturnsData()
    {
        var (content, images) = JournalRequestValidator.Validate("  Valid Content  ", new List<string> { " http://img.com  " });

        Assert.Equal("Valid Content", content);
        Assert.Single(images);
        Assert.Equal("http://img.com", images[0]);
    }

    [Fact]
    public void JournalRequestValidator_Validate_InvalidContent_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => JournalRequestValidator.Validate("", null));
    }

    [Fact]
    public void JournalRequestValidator_Validate_TooManyImages_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => JournalRequestValidator.Validate("Content", new List<string> { "1", "2", "3", "4", "5", "6" }));
    }

    [Fact]
    public async Task LearningPathItemValidator_BuildItems_Valid_ReturnsList()
    {
        var tutRepo = new Mock<ITutorialRepository>();
        var id = Guid.NewGuid();
        tutRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<Tutorial> { new Tutorial { Id = id, IsOfficial = true, Status = TutorialStatus.Published } });

        var items = await LearningPathItemValidator.BuildItemsAsync(tutRepo.Object, new List<Guid> { id }, default);

        Assert.Single(items);
        Assert.Equal(id, items[0].TutorialId);
    }
}
