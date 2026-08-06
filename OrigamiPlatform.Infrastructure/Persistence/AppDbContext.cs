using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    public DbSet<TutorialVariant> TutorialVariants => Set<TutorialVariant>(); // FT-11

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
    public DbSet<SePayWebhookLog> SePayWebhookLogs => Set<SePayWebhookLog>();

    // System
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BlockedWord> BlockedWords => Set<BlockedWord>();

    // Shop
    public DbSet<ShopLink> ShopLinks => Set<ShopLink>();

    // Learning Path (FT-33)
    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();
    public DbSet<LearningPathItem> LearningPathItems => Set<LearningPathItem>();
    public DbSet<LearningPathCompletion> LearningPathCompletions => Set<LearningPathCompletion>();

    // Learning Path Modes (tiered roadmap + unlock test)
    public DbSet<LearningPathMode> LearningPathModes => Set<LearningPathMode>();
    public DbSet<LearningPathModeUnlockTest> LearningPathModeUnlockTests => Set<LearningPathModeUnlockTest>();
    public DbSet<ModeUnlockSubmission> ModeUnlockSubmissions => Set<ModeUnlockSubmission>();

    // Clan
    public DbSet<Clan> Clans => Set<Clan>();
    public DbSet<ClanMember> ClanMembers => Set<ClanMember>();
    public DbSet<ClanInvite> ClanInvites => Set<ClanInvite>();

    // Gamification (FT-26 Streak, FT-27 Daily Quest, FT-28 Hạt Gấp)
    public DbSet<StreakLog> StreakLogs => Set<StreakLog>();
    public DbSet<DailyQuest> DailyQuests => Set<DailyQuest>();
    public DbSet<UserDailyQuestProgress> UserDailyQuestProgresses => Set<UserDailyQuestProgress>();
    public DbSet<HatGapTransaction> HatGapTransactions => Set<HatGapTransaction>();
    public DbSet<PaperPattern> PaperPatterns => Set<PaperPattern>(); // FT-28
    public DbSet<UserPaperPattern> UserPaperPatterns => Set<UserPaperPattern>(); // FT-28

    // Daily Challenge (FT-34)
    public DbSet<DailyChallenge> DailyChallenges => Set<DailyChallenge>();
    public DbSet<DailyChallengeSubmission> DailyChallengeSubmissions => Set<DailyChallengeSubmission>();
    public DbSet<ChallengeStreakLog> ChallengeStreakLogs => Set<ChallengeStreakLog>();

    // Weekly Challenge — mirror của Daily Challenge, chỉ mở Chủ Nhật, độ khó Advanced
    public DbSet<WeeklyChallenge> WeeklyChallenges => Set<WeeklyChallenge>();
    public DbSet<WeeklyChallengeSubmission> WeeklyChallengeSubmissions => Set<WeeklyChallengeSubmission>();

    // Badge System (FT-35)
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Toàn bộ DateTime trong hệ thống được lưu dưới dạng UTC (DateTime.UtcNow).
        // SQL Server "datetime2" không lưu Kind, nên khi EF Core đọc lại, Kind bị reset
        // về Unspecified. Khi đó System.Text.Json serialize thiếu hậu tố "Z", khiến
        // frontend (new Date(iso)) hiểu nhầm là giờ local -> lệch đúng bằng offset múi giờ
        // (7 giờ ở VN) trong mọi nơi hiển thị thời gian (vd "vừa tạo" hiện "7 giờ trước").
        // Gắn lại Kind=Utc ngay khi đọc từ DB để khắc phục tận gốc cho mọi entity.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var utcNullableConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(utcNullableConverter);
                }
            }
        }
    }
}
