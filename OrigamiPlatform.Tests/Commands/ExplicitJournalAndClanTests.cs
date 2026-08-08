using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Journals;
using OrigamiPlatform.Application.DTOs.Journals;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Application.Commands.Clan;

namespace OrigamiPlatform.Tests.Commands;

public class ExplicitJournalAndClanTests
{
    [Fact]
    public async Task CreateJournalHandler_ValidData_CreatesJournal()
    {
        var journals = new Mock<IJournalRepository>();
        var blocked = new Mock<IBlockedWordService>();
        
        var handler = new CreateJournalHandler(journals.Object, blocked.Object);
        var request = new CreateJournalRequest(null, "Valid Content", new List<string> { "http://img.com" }, true);
        
        blocked.Setup(x => x.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        var user = new User();
        var expectedJournal = new Journal { Id = Guid.NewGuid(), User = user, CreatedAt = DateTime.UtcNow, Content = "Valid Content" };
        
        journals.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(expectedJournal);
        
        var cmd = new CreateJournalCommand(Guid.NewGuid(), request);
        var result = await handler.HandleAsync(cmd);
        
        Assert.NotNull(result);
        Assert.Equal("Valid Content", result.Content);
        journals.Verify(x => x.AddAsync(It.IsAny<Journal>(), default), Times.Once);
    }
    
    [Fact]
    public async Task CreateClanHandler_ValidData_CreatesClan()
    {
        var clans = new Mock<IClanRepository>();
        var members = new Mock<IClanMemberRepository>();
        
        var handler = new CreateClanHandler(clans.Object, members.Object);
        var userId = Guid.NewGuid();
        
        members.Setup(x => x.GetByUserIdAsync(userId, default)).ReturnsAsync((ClanMember)null!);
        clans.Setup(x => x.GetByNameAsync("New Clan", default)).ReturnsAsync((global::OrigamiPlatform.Domain.Entities.Clan)null!);
        
        var clanMembers = new List<ClanMember> 
        { 
            new ClanMember { UserId = userId, User = new User() } 
        };
        members.Setup(x => x.GetByClanIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(clanMembers);
        
        var cmd = new CreateClanCommand(userId, "New Clan");
        var result = await handler.HandleAsync(cmd);
        
        Assert.NotNull(result);
        Assert.Equal("New Clan", result.Name);
        clans.Verify(x => x.AddAsync(It.IsAny<global::OrigamiPlatform.Domain.Entities.Clan>(), default), Times.Once);
        members.Verify(x => x.AddAsync(It.IsAny<ClanMember>(), default), Times.Once);
    }
}
