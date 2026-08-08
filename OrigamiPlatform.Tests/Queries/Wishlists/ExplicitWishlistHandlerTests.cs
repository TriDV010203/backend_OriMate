using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Wishlists;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Application.DTOs.Common;

namespace OrigamiPlatform.Tests.Queries.Wishlists;

public class ExplicitWishlistHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidWishlist_MapsCorrectly()
    {
        var wishlists = new Mock<IWishlistRepository>();
        var tutorials = new Mock<ITutorialRepository>();
        var posts = new Mock<ICommunityPostRepository>();
        
        var handler = new GetWishlistHandler(wishlists.Object, tutorials.Object, posts.Object);
        var tutId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        var wishlistItems = new List<Wishlist>
        {
            new Wishlist { TargetId = tutId, TargetType = TargetType.Tutorial, CreatedAt = DateTime.UtcNow },
            new Wishlist { TargetId = postId, TargetType = TargetType.CommunityPost, CreatedAt = DateTime.UtcNow }
        };
        
        wishlists.Setup(x => x.GetUserWishlistAsync(It.IsAny<Guid>(), It.IsAny<TargetType?>(), 1, 10, default))
            .ReturnsAsync(new PagedResult<Wishlist>(wishlistItems, 2, 1, 10, 1));
            
        tutorials.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<Tutorial> { 
                new Tutorial 
                { 
                    Id = tutId, Title = "Tut", Slug = "tut", Category = new Category { Name = "Cat" }, 
                    Author = new User { Email = "test@test.com" } 
                } 
            });
            
        posts.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<CommunityPost> {
                new CommunityPost { Id = postId, Content = "Post", Author = new User() }
            });
            
        var query = new GetWishlistQuery(Guid.NewGuid(), null, 1, 10);
        var result = await handler.HandleAsync(query);
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.NotNull(result.Items.ElementAt(0).Tutorial);
        Assert.NotNull(result.Items.ElementAt(1).CommunityPost);
    }
}
