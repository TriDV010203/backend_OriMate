# FT_MAPPING_v5.md — OriMate (theo BRD v5.0)

## References
- `BE_ARCHITECTURE.md` — solution structure & layer diagram (cần cập nhật theo file này)
- `BE_PROJECT_RULES.md` — coding rules & patterns (vẫn giữ nguyên, chỉ đổi phần Feature Structure & Ownership)
- `MVP_SCOPE.md` — phạm vi 3 tuần code (Must/Should/Won't)

⚠️ File này **thay thế hoàn toàn** bảng "Feature → FT mapping" trong `BE_ARCHITECTURE.md` (mục 4) và bảng ownership trong `BE_PROJECT_RULES.md` (mục 9), vì FT numbering đã đổi từ FT-01→18 (bản cũ, còn Advertising/Family Plan) sang FT-01→33 (BRD v5.0).

---

## 1. Feature Folder ↔ FT ↔ Entity ↔ Actor chính

### FE-01 Account & Auth
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-01 | Register/Login/JWT | `Auth` | `AuthController` | `User`, `UserRole` | Guest, User |
| FT-02 | Email verify & reset | `Auth` | `AuthController` | `User`, `EmailLog` | Guest, User |
| FT-03 | Admin config | `AdminConfiguration` | `AdminController` | `Category`, `BlockedWord` | Admin |

### FE-02 Tutorial Lifecycle
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-04 | Authoring | `Tutorials` | `TutorialController` | `Tutorial`, `TutorialStep` | Creator |
| FT-05 | Manager review | `Tutorials` | `ReviewController` | `TutorialReviewHistory` (immutable) | Manager |
| FT-06 | Search/Filter/VIP gating | `Tutorials` | `TutorialController` | `Tutorial` | Guest, User |
| FT-07 | Edit after publish | `Tutorials` | `TutorialController` | `Tutorial` (working copy) | Creator, Manager |
| FT-08 | SEO & feed ranking | `Tutorials` | `TutorialController` | `Tutorial` | Guest, User |

### FE-03 Learning
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-09 | Step progress | `Learning` | `LearningController` | `StepProgress` | User |
| FT-10 | Stuck button | `Learning` | `LearningController` | `StuckThread`, `Comment` | User |
| FT-11 | Tutorial variants | `Tutorials` | `TutorialController` | `TutorialVariant` | Creator |

### FE-04 Community
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-12 | Post/Like/Report | `Community` | `CommunityController` | `CommunityPost`, `Like`, `Report` | User |
| FT-13 | Comment/Wishlist/Follow/Notification | `Community` | `CommunityController` | `Comment`, `Wishlist`, `FollowRelationship`, `Notification` | User |
| FT-14 | Moderation | `Moderation` | `ModerationController` | `Report`, `AuditLog` | CTV, Manager, Admin |
| FT-15 | Creator profile & feed | `Community` | `CommunityController` | `UserProfile` | Creator |

### FE-05 Monetisation
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-16 | VIP subscription | `VipSubscription` + `Payment` | `SubscriptionController` | `VipSubscription`, `Transaction`, `CreatorVipSettings` | User, Creator |
| FT-17 | Creator dashboard | `VipSubscription` | `SubscriptionController` | `Transaction` (aggregate) | Creator |
| FT-18 | Shop affiliate | `Shop` | `ShopController` | `ShopLink` | Guest, User |

### FE-06 Personal Growth
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-19 | Achievement & Portfolio | `Achievement` | `AchievementController` | `Achievement` | User |
| FT-20 | Personal Milestone | `Gamification` | `GamificationController` | `PersonalMilestone` | User |
| FT-21 | Folding Journal | `Journal` | `JournalController` | `Journal` | User |

### FE-07 Clan & Competition
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-22 | Clan | `Clan` | `ClanController` | `Clan`, `ClanMember`, `ClanInvite` | User |
| FT-23 | Weekly Challenge & pairwise | `Challenge` | `ChallengeController` | `WeeklyChallenge`, `ChallengeSubmission`, `ChallengeVote` | CTV, User |
| FT-24 | Clan Quest & League | `Clan` | `ClanController` | `ClanQuest`, `LeaguePoints` | User |

### FE-08 Gamification
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-25 | Skill Level | `Gamification` | `GamificationController` | `SkillPoints` (derived) | User |
| FT-26 | Daily Streak | `Gamification` | `GamificationController` | `StreakLog` | User |
| FT-27 | Daily Quest 2 pool | `Gamification` | `GamificationController` | `DailyQuest`, `UserDailyQuestProgress` | User |
| FT-28 | Hạt Gấp & Paper Pattern | `Gamification` | `GamificationController` | `HatGapTransaction`, `PaperPattern` | User |

### FE-09/10/11/12 — Onboarding, Re-engagement, Discovery, Learning Path
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-29 | First-run flow | `Onboarding` | `OnboardingController` | (dùng lại `User`, `UserProfile`) | User mới |
| FT-30 | Push/email triggers | `Reengagement` | (background job, không cần controller riêng) | `EmailLog`, `Notification` | User |
| FT-31 | Trending/Continue/Next Fold/Category | `Discovery` | `DiscoveryController` | (query tổng hợp, không entity riêng) | Guest, User |
| FT-32 | Official OriMate tutorials (seed) | `Tutorials` | `TutorialController` | `Tutorial` (isOfficial flag) | Admin |
| FT-33 | Curated Learning Path | `LearningPath` | `LearningPathController` | `LearningPath`, `LearningPathItem` | Admin |

---

## 2. Entity cần XOÁ khỏi Domain (thuộc scope cũ đã bỏ)

```
FamilySubscription.cs
FamilyProject.cs
FamilyProjectMember.cs
FamilyProjectStepProgress.cs
AdPlacement.cs
AdCampaign.cs
AdBanner.cs
AdImpression.cs
AdClick.cs
```
→ Kèm theo: `FamilyProjectController.cs`, `AdCampaignController.cs`, feature folder `FamilyProject/`, `Advertisement/`, job `AdBudgetDepletionJob.cs`, role `AdvertisingPartner`.

## 3. Entity cần THÊM MỚI vào Domain

```
Clan.cs / ClanMember.cs / ClanInvite.cs / ClanQuest.cs
WeeklyChallenge.cs / ChallengeSubmission.cs / ChallengeVote.cs
DailyQuest.cs / UserDailyQuestProgress.cs
StreakLog.cs
HatGapTransaction.cs / PaperPattern.cs
SkillPoints.cs (hoặc field derived trên UserProfile)
PersonalMilestone.cs
StepProgress.cs / StuckThread.cs
ShopLink.cs
LearningPath.cs / LearningPathItem.cs
```

## 4. Background Jobs — cập nhật

| Job cũ | Trạng thái | Job mới cần thêm |
|---|---|---|
| `SubscriptionExpiryJob` | Giữ nguyên (đổi field cho khớp VIP mới, bỏ phần Family) | — |
| `AdBudgetDepletionJob` | ❌ Xoá | — |
| — | — | `DailyQuestResetJob` (00:00 GMT+7) |
| — | — | `ClanQuestResetJob` (Thứ 2 00:00) |
| — | — | `LeagueResetJob` (ngày 1 hàng tháng) |
| — | — | `ChallengeResultJob` (CN 08:00) — *chỉ cần nếu FT-23 nằm trong scope 3 tuần, xem MVP_SCOPE.md* |

## 5. Vấn đề chưa chốt — cần quyết định trước khi code FT-16

`Payment` feature hiện đang khớp hướng **"Transaction confirm flow"** (Admin xác nhận thủ công), **không phải** SePay webhook như BRD v5.0 bản text đang mô tả. Cần chốt 1 trong 2 hướng trước khi viết `PaymentService`/`SubscriptionService` — vì kéo theo khác biệt lớn về entity `Transaction` (có `SePaySignature`, `WebhookPayload` hay không) và endpoint (`/webhooks/sepay` có tồn tại hay không).
