using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Application.DTOs.AdminConfiguration;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.IntegrationTests;

public sealed class HttpFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Email = "bf01-flow@example.com";
    private const string Password = "Password@123";
    private const string Slug = "bf01-vip-origami";
    private const string PaymentCode = "OMVIP0123456789ABCDEF0123456789ABCDEF";

    private readonly CustomWebApplicationFactory _factory;

    public HttpFlowIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    // ── BF01: Registration, payment and VIP access ─────────────────────────

    [Fact]
    public async Task BF01_UserRegistration_Payment_And_VipAccess_Flow()
    {
        await _factory.ResetDatabaseAsync();
        var seededData = await SeedDataAsync();
        using var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email,
            Password,
            DisplayName = "BF01 User"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registration = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registration.Should().NotBeNull();

        await ActivateRegisteredUserAsync(registration!.UserId, seededData.TransactionId);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email,
            Password
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        login.Should().NotBeNull();
        login!.Token.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.Token);

        var lockedResponse = await client.GetAsync($"/api/tutorials/{Slug}");
        lockedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lockedTutorial = await lockedResponse.Content.ReadFromJsonAsync<TutorialDetailDto>();
        lockedTutorial.Should().NotBeNull();
        lockedTutorial!.IsVipLocked.Should().BeTrue();
        lockedTutorial.Steps.ElementAt(0).Description.Should().Be("Step 1 content");
        lockedTutorial.Steps.ElementAt(1).Description.Should().Be("Step 2 content");
        lockedTutorial.Steps.ElementAt(2).Description.Should().BeEmpty();
        lockedTutorial.Steps.ElementAt(2).ImageUrl.Should().BeNull();
        lockedTutorial.Steps.ElementAt(2).IsLocked.Should().BeTrue();

        var webhookResponse = await SendWebhookAsync(client, seededData.TransactionId);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await webhookResponse.Content.ReadFromJsonAsync<SuccessResponse>())!.Success.Should().BeTrue();

        var unlockedResponse = await client.GetAsync($"/api/tutorials/{Slug}");
        unlockedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unlockedTutorial = await unlockedResponse.Content.ReadFromJsonAsync<TutorialDetailDto>();
        unlockedTutorial.Should().NotBeNull();
        unlockedTutorial!.IsVipLocked.Should().BeFalse();
        unlockedTutorial.Steps.All(step =>
            !step.IsLocked && !string.IsNullOrWhiteSpace(step.Description) && step.ImageUrl is not null)
            .Should().BeTrue();
    }

    // ── Tutorial publishing and approval flow ──────────────────────────────

    [Fact]
    public async Task Should_Complete_Tutorial_Approval_Flow_Successfully()
    {
        await _factory.ResetDatabaseAsync();
        var users = await SeedPublishingUsersAsync();
        using var creatorClient = await CreateAuthenticatedClientAsync(users.CreatorEmail);

        var createResponse = await creatorClient.PostAsync("/api/tutorials", CreateJsonContent(CreateTutorialPayload()));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<TutorialResponse>(createResponse);
        created.Status.Should().Be(nameof(TutorialStatus.Draft));

        var submitResponse = await creatorClient.PutAsync($"/api/tutorials/{created.Id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitted = await ReadJsonAsync<TutorialResponse>(submitResponse);
        submitted.Status.Should().Be(nameof(TutorialStatus.PendingManagerReview));

        using var managerClient = await CreateAuthenticatedClientAsync(users.ManagerEmail);
        var publishResponse = await managerClient.PutAsync($"/api/tutorials/{created.Id}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertTutorialStatusAsync(created.Id, TutorialStatus.Published);

        // The public detail endpoint uses the tutorial slug, not its GUID.
        using var guestClient = _factory.CreateClient();
        var publicResponse = await guestClient.GetAsync($"/api/tutorials/{created.Slug}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Title_Is_Missing_On_Create()
    {
        await _factory.ResetDatabaseAsync();
        var users = await SeedPublishingUsersAsync();
        using var client = await CreateAuthenticatedClientAsync(users.CreatorEmail);
        var payload = CreateTutorialPayload();
        payload.Remove("title");

        var response = await client.PostAsync("/api/tutorials", CreateJsonContent(payload));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_Customer_Tries_To_Submit_Tutorial()
    {
        await _factory.ResetDatabaseAsync();
        var users = await SeedPublishingUsersAsync();
        var tutorial = await SeedPublishingTutorialAsync(users.CreatorId, TutorialStatus.Draft);
        using var customerClient = await CreateAuthenticatedClientAsync(users.CustomerEmail);

        var response = await customerClient.PutAsync($"/api/tutorials/{tutorial.Id}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_Creator_Submits_Another_Creators_Tutorial()
    {
        await _factory.ResetDatabaseAsync();
        var users = await SeedPublishingUsersAsync();
        var tutorial = await SeedPublishingTutorialAsync(users.OtherCreatorId, TutorialStatus.Draft);
        using var creatorClient = await CreateAuthenticatedClientAsync(users.CreatorEmail);

        var response = await creatorClient.PutAsync($"/api/tutorials/{tutorial.Id}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Manager_Publishes_A_Draft_Tutorial()
    {
        await _factory.ResetDatabaseAsync();
        var users = await SeedPublishingUsersAsync();
        var tutorial = await SeedPublishingTutorialAsync(users.CreatorId, TutorialStatus.Draft);
        using var managerClient = await CreateAuthenticatedClientAsync(users.ManagerEmail);

        var response = await managerClient.PutAsync($"/api/tutorials/{tutorial.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_Customer_Tries_To_Publish_Tutorial()
    {
        await _factory.ResetDatabaseAsync();
        var users = await SeedPublishingUsersAsync();
        var tutorial = await SeedPublishingTutorialAsync(users.CreatorId, TutorialStatus.PendingManagerReview);
        using var customerClient = await CreateAuthenticatedClientAsync(users.CustomerEmail);

        var response = await customerClient.PutAsync($"/api/tutorials/{tutorial.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Submitting_Tutorial_Without_Required_Content()
    {
        await _factory.ResetDatabaseAsync();
        var users = await SeedPublishingUsersAsync();
        var tutorial = await SeedPublishingTutorialAsync(users.CreatorId, TutorialStatus.Draft, false);
        using var creatorClient = await CreateAuthenticatedClientAsync(users.CreatorEmail);

        var response = await creatorClient.PutAsync($"/api/tutorials/{tutorial.Id}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Return_Unauthorized_When_Guest_Creates_Tutorial()
    {
        await _factory.ResetDatabaseAsync();
        await SeedPublishingUsersAsync();
        using var guestClient = _factory.CreateClient();

        var response = await guestClient.PostAsync("/api/tutorials", CreateJsonContent(CreateTutorialPayload()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Publishing_Tutorial_That_Is_Already_Published()
    {
        await _factory.ResetDatabaseAsync();
        var users = await SeedPublishingUsersAsync();
        var tutorial = await SeedPublishingTutorialAsync(users.CreatorId, TutorialStatus.Published);
        using var managerClient = await CreateAuthenticatedClientAsync(users.ManagerEmail);

        var response = await managerClient.PutAsync($"/api/tutorials/{tutorial.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── TC-HTTP-ADM: Category and user administration ──────────────────────

    [Fact]
    public async Task TC_HTTP_ADM_001_CreateCategory_WithValidPayload_ReturnsActiveCategory()
    {
        await _factory.ResetDatabaseAsync();
        var adminData = await SeedAdminDataAsync();
        using var adminClient = await CreateAuthenticatedClientAsync(adminData.AdminEmail);
        var categoryName = $"Admin Category {Guid.NewGuid():N}";

        // Actual route is /api/admin/categories and the controller returns 200 OK,
        // although the original test matrix listed /api/categories and 201 Created.
        var response = await adminClient.PostAsync(
            "/api/admin/categories",
            CreateJsonContent(new { name = categoryName }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await ReadJsonAsync<CategoryResponse>(response);
        created.Name.Should().Be(categoryName);
        created.IsActive.Should().BeTrue();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Categories.SingleAsync(category => category.Id == created.Id);
        saved.IsActive.Should().BeTrue();
        saved.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task TC_HTTP_ADM_002_CreateCategory_WithDuplicateName_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();
        var adminData = await SeedAdminDataAsync();
        var categoryName = $"Duplicate-{Guid.NewGuid():N}";
        await SeedCategoryAsync(categoryName);
        using var adminClient = await CreateAuthenticatedClientAsync(adminData.AdminEmail);

        var response = await adminClient.PostAsync(
            "/api/admin/categories",
            CreateJsonContent(new { name = categoryName }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TC_HTTP_ADM_003_DeleteCategory_WithTutorialReference_SoftDeletesToProtectForeignKey()
    {
        await _factory.ResetDatabaseAsync();
        var adminData = await SeedAdminDataAsync(includeReferencedTutorial: true);
        using var adminClient = await CreateAuthenticatedClientAsync(adminData.AdminEmail);

        // The handler intentionally protects the FK by soft-deleting the category;
        // therefore the current contract is 200 OK, not 400 BadRequest.
        var response = await adminClient.DeleteAsync($"/api/admin/categories/{adminData.CategoryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await db.Categories.SingleAsync(item => item.Id == adminData.CategoryId);
        category.IsActive.Should().BeFalse();
        category.IsDeleted.Should().BeTrue();
        (await db.Tutorials.CountAsync(item => item.CategoryId == adminData.CategoryId)).Should().Be(1);
    }

    [Fact]
    public async Task TC_HTTP_ADM_005_SuspendUser_ReturnsOkAndChangesAccountStatusToSuspended()
    {
        await _factory.ResetDatabaseAsync();
        var adminData = await SeedAdminDataAsync();
        using var adminClient = await CreateAuthenticatedClientAsync(adminData.AdminEmail);

        // There is no /block endpoint in the current API. Blocking is represented
        // by PUT /api/admin/users/{id}/suspend and a Suspended account status.
        var response = await adminClient.PutAsync(
            $"/api/admin/users/{adminData.TargetUserId}/suspend",
            CreateJsonContent(new { reason = "Repeated policy violations" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(item => item.Id == adminData.TargetUserId);
        user.Status.Should().Be(AccountStatus.Suspended);
    }

    // ── Publishing-flow helpers ─────────────────────────────────────────────

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/login", CreateJsonContent(new { email, password = Password }));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await ReadJsonAsync<AuthResponse>(response);
        auth.Token.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    private async Task<PublishingUsers> SeedPublishingUsersAsync()
    {
        const int categoryId = 9201;
        var now = DateTime.UtcNow;
        var creator = NewPublishingUser("creator", now);
        var manager = NewPublishingUser("manager", now);
        var customer = NewPublishingUser("customer", now);
        var otherCreator = NewPublishingUser("other-creator", now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        foreach (var user in new[] { creator, manager, customer, otherCreator })
            user.PasswordHash = hasher.Hash(Password);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Categories.Add(new Category
        {
            Id = categoryId,
            Name = "Publishing Flow Category",
            IsActive = true,
            CreatedAt = now
        });
        db.Users.AddRange(creator, manager, customer, otherCreator);
        db.UserProfiles.AddRange(
            PublishingProfile(creator, "Creator"),
            PublishingProfile(manager, "Manager"),
            PublishingProfile(customer, "Customer"),
            PublishingProfile(otherCreator, "Other Creator"));
        db.UserRoles.AddRange(
            PublishingRole(creator, UserRoleType.User, now),
            PublishingRole(manager, UserRoleType.Manager, now),
            PublishingRole(customer, UserRoleType.User, now),
            PublishingRole(otherCreator, UserRoleType.User, now));
        await db.SaveChangesAsync();

        return new PublishingUsers(
            creator.Id, manager.Id, customer.Id, otherCreator.Id,
            creator.Email, manager.Email, customer.Email);
    }

    private async Task<Tutorial> SeedPublishingTutorialAsync(Guid authorId, TutorialStatus status, bool includeValidContent = true)
    {
        const int categoryId = 9201;
        var now = DateTime.UtcNow;
        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            CategoryId = categoryId,
            Title = "Seeded Publishing Tutorial",
            Description = "A complete tutorial used by publishing flow integration tests.",
            Slug = $"seeded-publishing-{Guid.NewGuid():N}",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = status,
            PublishedAt = status == TutorialStatus.Published ? now : null,
            CreatedAt = now,
            CoverImageUrl = "https://example.com/cover.png",
            Steps = includeValidContent
                ? Enumerable.Range(1, 3).Select(step => new TutorialStep
                {
                    Id = Guid.NewGuid(),
                    StepOrder = step,
                    Description = $"Step {step} content for integration testing.",
                    ImageUrl = $"https://example.com/step-{step}.png",
                    CreatedAt = now
                }).ToList()
                : new List<TutorialStep>()
        };

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tutorials.Add(tutorial);
        await db.SaveChangesAsync();
        return tutorial;
    }

    private async Task AssertTutorialStatusAsync(Guid tutorialId, TutorialStatus expectedStatus)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tutorial = await db.Tutorials.SingleAsync(item => item.Id == tutorialId);
        tutorial.Status.Should().Be(expectedStatus);
    }

    private async Task<AdminData> SeedAdminDataAsync(bool includeReferencedTutorial = false)
    {
        var now = DateTime.UtcNow;
        var admin = NewPublishingUser("admin", now);
        var targetUser = NewPublishingUser("target-user", now);
        var categoryId = Random.Shared.Next(100000, 900000);
        var category = new Category
        {
            Id = categoryId,
            Name = $"Seeded Admin Category {Guid.NewGuid():N}",
            IsActive = true,
            CreatedAt = now
        };

        await using var scope = _factory.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        admin.PasswordHash = hasher.Hash(Password);
        targetUser.PasswordHash = hasher.Hash(Password);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.AddRange(admin, targetUser);
        db.UserProfiles.AddRange(
            PublishingProfile(admin, "Admin"),
            PublishingProfile(targetUser, "Target User"));
        db.UserRoles.AddRange(
            PublishingRole(admin, UserRoleType.Admin, now),
            PublishingRole(targetUser, UserRoleType.User, now));
        db.Categories.Add(category);

        if (includeReferencedTutorial)
        {
            db.Tutorials.Add(new Tutorial
            {
                Id = Guid.NewGuid(),
                AuthorId = targetUser.Id,
                CategoryId = categoryId,
                Title = "Referenced Tutorial",
                Description = "A tutorial that keeps the category referenced during deletion.",
                Slug = $"referenced-{Guid.NewGuid():N}",
                Type = TutorialType.Free,
                Status = TutorialStatus.Published,
                Difficulty = TutorialDifficulty.Beginner,
                PublishedAt = now,
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync();
        return new AdminData(admin.Email, targetUser.Id, categoryId);
    }

    private async Task<Category> SeedCategoryAsync(string name)
    {
        var category = new Category
        {
            Id = Random.Shared.Next(100000, 900000),
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static User NewPublishingUser(string prefix, DateTime createdAt)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = $"{prefix}-{Guid.NewGuid():N}@example.com",
            Status = AccountStatus.Active,
            CreatedAt = createdAt
        };

    private static UserProfile PublishingProfile(User user, string displayName)
        => new() { UserId = user.Id, DisplayName = displayName, CreatedAt = user.CreatedAt };

    private static UserRole PublishingRole(User user, UserRoleType role, DateTime createdAt)
        => new() { UserId = user.Id, Role = role, CreatedAt = createdAt };

    private static Dictionary<string, object?> CreateTutorialPayload()
        => new()
        {
            ["title"] = "A Valid Origami Tutorial",
            ["description"] = "A complete description that satisfies the publishing flow validation rules.",
            ["categoryId"] = 9201,
            ["difficulty"] = "Beginner",
            ["type"] = "Free",
            ["coverImageUrl"] = "https://example.com/cover.png",
            ["steps"] = new[]
            {
                new { stepOrder = 1, description = "Fold the paper in half.", imageUrl = "https://example.com/1.png" },
                new { stepOrder = 2, description = "Open and crease the paper.", imageUrl = "https://example.com/2.png" },
                new { stepOrder = 3, description = "Finish the origami figure.", imageUrl = "https://example.com/3.png" }
            }
        };

    private static StringContent CreateJsonContent(object value)
        => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
        => (await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;

    private sealed record PublishingUsers(
        Guid CreatorId,
        Guid ManagerId,
        Guid CustomerId,
        Guid OtherCreatorId,
        string CreatorEmail,
        string ManagerEmail,
        string CustomerEmail);

    private sealed record AdminData(string AdminEmail, Guid TargetUserId, int CategoryId);

    private async Task<SeededData> SeedDataAsync()
    {
        var now = DateTime.UtcNow;
        var creatorId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var category = new Category
        {
            Id = Random.Shared.Next(1000, 100000),
            Name = "BF01 Category",
            IsActive = true,
            CreatedAt = now
        };
        var creator = new User
        {
            Id = creatorId,
            Email = $"bf01-creator-{Guid.NewGuid():N}@example.com",
            PasswordHash = "unused",
            Status = AccountStatus.Active,
            CreatedAt = now,
            Profile = new UserProfile
            {
                UserId = creatorId,
                DisplayName = "BF01 Creator",
                CreatedAt = now
            }
        };
        var tutorial = new Tutorial
        {
            Id = tutorialId,
            AuthorId = creatorId,
            Author = creator,
            CategoryId = category.Id,
            Category = category,
            Title = "BF01 VIP Origami",
            Description = "A VIP tutorial for the HTTP flow test.",
            Slug = Slug,
            Type = TutorialType.VIP,
            Status = TutorialStatus.Published,
            Difficulty = TutorialDifficulty.Beginner,
            PublishedAt = now,
            CreatedAt = now,
            Steps = Enumerable.Range(1, 4).Select(step => new TutorialStep
            {
                Id = Guid.NewGuid(),
                TutorialId = tutorialId,
                StepOrder = step,
                Description = $"Step {step} content",
                ImageUrl = $"https://example.com/bf01-{step}.png",
                CreatedAt = now
            }).ToList()
        };
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty,
            CreatorId = creatorId,
            Creator = creator,
            TransactionType = TransactionType.VipSubscription,
            Amount = 30000m,
            PlatformFeeAmount = 3000m,
            CreatorNetAmount = 27000m,
            Status = TransactionStatus.PendingConfirmation,
            PaymentCode = PaymentCode,
            CreatedAt = now
        };

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Categories.Add(category);
        db.Users.Add(creator);
        db.Tutorials.Add(tutorial);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return new SeededData(transaction.Id, creatorId);
    }

    private async Task ActivateRegisteredUserAsync(Guid userId, Guid transactionId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(user => user.Id == userId);
        user.Status = AccountStatus.Active;
        user.VerificationToken = null;
        user.TokenExpiry = null;
        var transaction = await db.Transactions.SingleAsync(item => item.Id == transactionId);
        transaction.UserId = userId;
        await db.SaveChangesAsync();
    }

    private async Task<HttpResponseMessage> SendWebhookAsync(HttpClient client, Guid transactionId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var transaction = await db.Transactions.SingleAsync(item => item.Id == transactionId);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/sepay")
        {
            Content = JsonContent.Create(new
            {
                id = 810001,
                gateway = "MBBank",
                transactionDate = "2026-09-15 10:00:00",
                accountNumber = "4238659887986",
                subAccount = "",
                code = transaction.PaymentCode,
                content = transaction.PaymentCode,
                transferType = "in",
                transferAmount = transaction.Amount,
                accumulated = 0,
                referenceCode = "BF01-REF",
                description = "BF01 integration payment"
            })
        };
        request.Headers.Add("Authorization", "Apikey Orimate2026");
        return await client.SendAsync(request);
    }

    private sealed record SeededData(Guid TransactionId, Guid CreatorId);

    private sealed record SuccessResponse(bool Success);
}