using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Users
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // Tutorials
    public DbSet<Tutorial> Tutorials => Set<Tutorial>();
    public DbSet<TutorialStep> TutorialSteps => Set<TutorialStep>();
    public DbSet<TutorialReviewHistory> TutorialReviewHistories => Set<TutorialReviewHistory>();
    public DbSet<Category> Categories => Set<Category>();

    // Community
    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<CommunityPostMedia> CommunityPostMedias => Set<CommunityPostMedia>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Report> Reports => Set<Report>();

    // Social
    public DbSet<FollowRelationship> FollowRelationships => Set<FollowRelationship>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Content & Progress
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<Journal> Journals => Set<Journal>();
    public DbSet<TutorialStepProgress> TutorialStepProgresses => Set<TutorialStepProgress>();
    public DbSet<StuckThread> StuckThreads => Set<StuckThread>();
    public DbSet<PersonalMilestone> PersonalMilestones => Set<PersonalMilestone>();

    // Subscriptions & Payments
    public DbSet<VipSubscription> VipSubscriptions => Set<VipSubscription>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CreatorVipSettings> CreatorVipSettings => Set<CreatorVipSettings>();

    // System
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BlockedWord> BlockedWords => Set<BlockedWord>();

    // Shop
    public DbSet<ShopLink> ShopLinks => Set<ShopLink>();

    // Clan
    public DbSet<Clan> Clans => Set<Clan>();
    public DbSet<ClanMember> ClanMembers => Set<ClanMember>();
    public DbSet<ClanInvite> ClanInvites => Set<ClanInvite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
