# FT_MAPPING_v5.md — OriMate (theo BRD v5.0)

## References
- `Report_1_Vision_Scope_v2.1.docx` — **nguồn FE chính thức duy nhất** (Vision & Scope, đã PO/GVHD ký hoặc đang review). File này KHÔNG tự đặt ra FE riêng nữa — mọi FT bên dưới đều map vào đúng 8 FE đã khai báo ở đó.
- `BE_ARCHITECTURE.md` — solution structure & layer diagram
- `BE_PROJECT_RULES.md` — coding rules & patterns
- `MVP_SCOPE.md` — phạm vi 3 tuần code (Must/Should/Won't)
- `Hướng_dẫn_Thu_thập_&_Xác_định_Yêu_cầu_Phần_mềm.docx` — quy tắc FE/FT dùng làm chuẩn đối chiếu

⚠️ **Đổi quan trọng so với bản trước:** bản cũ của file này tự đánh số FE-01→FE-12 riêng, trùng nhãn nhưng khác nội dung với FE-01→FE-07 trong Vision & Scope — gây lệch traceability. Bản này sửa lại: **8 FE-01→FE-08 dưới đây copy y nguyên từ `Report_1_Vision_Scope_v2.1.docx`**, không tự tạo thêm FE nào. Nếu Vision & Scope đổi FE (qua Change Request), file này phải update theo, không phải ngược lại.

---

## 1. Feature Folder ↔ FT ↔ Entity ↔ Actor chính

### FE-01: User Registration, Login & Platform Administration *(Must-have)*
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-01 | Register/Login/JWT | `Auth` | `AuthController` | `User`, `UserRole` | Guest, User |
| FT-02 | Email verify & reset | `Auth` | `AuthController` | `User`, `EmailLog` | Guest, User |
| FT-03 | Admin config | `AdminConfiguration` | `AdminController` | `Category`, `BlockedWord` | Admin |

### FE-02: Tutorial Publishing, Review & Guided Learning *(Must-have)*
*Gộp cả vòng đời tutorial lẫn trải nghiệm học theo bước — trước đây tách thành 2 nhóm "Tutorial Lifecycle" + "Learning" riêng, nay hợp nhất đúng theo FE-02 đã khai báo trong Vision & Scope.*

| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-04 | Authoring | `Tutorials` | `TutorialController` | `Tutorial`, `TutorialStep` | Creator |
| FT-05 | Manager review | `Tutorials` | `ReviewController` | `TutorialReviewHistory` (immutable) | Manager |
| FT-06 | Search/Filter/VIP gating | `Tutorials` | `TutorialController` | `Tutorial` | Guest, User |
| FT-07 | Edit after publish | `Tutorials` | `TutorialController` | `Tutorial` (working copy) | Creator, Manager |
| FT-08 | SEO & feed ranking | `Tutorials` | `TutorialController` | `Tutorial` | Guest, User |
| FT-09 | Step progress | `Learning` | `LearningController` | `StepProgress` | User |
| FT-10 | Stuck button | `Learning` | `LearningController` | `StuckThread`, `Comment` | User |
| FT-11 | Tutorial variants | `Tutorials` | `TutorialController` | `TutorialVariant` | Creator |

### FE-03: Community Feed, Social Interaction & Content Moderation *(Must-have)*
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-12 | Post/Like/Report | `Community` | `CommunityController` | `CommunityPost`, `Like`, `Report` | User |
| FT-13 | Comment/Wishlist/Follow/Notification | `Community` | `CommunityController` | `Comment`, `Wishlist`, `FollowRelationship`, `Notification` | User |
| FT-14 | Moderation | `Moderation` | `ModerationController` | `Report`, `AuditLog` | Contributor Reviewer, Manager, Admin |
| FT-15 | Creator profile & feed | `Community` | `CommunityController` | `UserProfile` | Creator |

### FE-04: Creator VIP Subscription, Monetisation & Shop *(Must-have)*
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-16 | VIP subscription | `VipSubscription` + `Payment` | `SubscriptionController` | `VipSubscription`, `Transaction`, `CreatorVipSettings` | User, Creator |
| FT-17 | Creator dashboard | `VipSubscription` | `SubscriptionController` | `Transaction` (aggregate) | Creator |
| FT-18 | Shop affiliate | `Shop` | `ShopController` | `ShopLink` | Guest, User |

### FE-05: Personal Achievement Tracking & Journal *(Should-have)*
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-19 | Achievement & Portfolio | `Achievements` | `AchievementsController` | `Achievement` | User |
| FT-20 | Personal Milestone | `Achievements` | `AchievementsController` | `PersonalMilestone` | User — dùng chung controller với FT-19 (trigger trực tiếp trong CreateAchievementHandler, không tách Gamification như dự kiến ban đầu) |
| FT-21 | Folding Journal | `Journal` | `JournalController` | `Journal` | User |

### FE-06: Clan Membership & Weekly Challenge *(Should-have)*
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-22 | Clan | `Clan` | `ClanController` | `Clan`, `ClanMember`, `ClanInvite` | User |
| FT-23 | Weekly Challenge & pairwise | `Challenge` | `ChallengeController` | `WeeklyChallenge`, `ChallengeSubmission`, `ChallengeVote` | Contributor Reviewer, User |
| FT-24 | Clan Quest & League | `Clan` | `ClanController` | `ClanQuest`, `LeaguePoints` | User |

### FE-07: Individual Gamification & Skill Progression *(Should-have)*
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-25 | Skill Level | `Gamification` | `GamificationController` | `SkillPoints` (derived) | User |
| FT-26 | Daily Streak | `Gamification` | `GamificationController` | `StreakLog` | User |
| FT-27 | Daily Quest | `Gamification` | `GamificationController` | `DailyQuest`, `UserDailyQuestProgress` | User |
| FT-28 | Hạt Gấp & Paper Pattern | `Gamification` | `GamificationController` | `HatGapTransaction`, `PaperPattern` | User |

### FE-08: Personalised Discovery & Onboarding *(Could-have)*
| FT | Tên | Feature folder | Controller | Entity chính | Actor |
|---|---|---|---|---|---|
| FT-29 | First-run flow | `Onboarding` | `OnboardingController` | (dùng lại `User`, `UserProfile`) | User mới |
| FT-30 | Push/email triggers | `Reengagement` | (background job, không cần controller riêng) | `EmailLog`, `Notification` | User |
| FT-31 | Trending/Continue/Next Fold/Category | `Discovery` | `DiscoveryController` | (query tổng hợp, không entity riêng) | Guest, User |
| FT-32 | Official OriMate tutorials (seed) | `Tutorials` | `TutorialController` | `Tutorial` (isOfficial flag) | Admin |
| FT-33 | Curated Learning Path | `LearningPath` | `LearningPathController` | `LearningPath`, `LearningPathItem` | Admin |

---

## 2. Kiểm tra tổng số FT

8 FE × số FT = 3 + 8 + 4 + 3 + 3 + 3 + 4 + 5 = **33 FT** — khớp đúng FT-01 → FT-33, không thiếu không thừa so với Vision & Scope.

## 3. Entity cần XOÁ khỏi Domain (thuộc scope cũ đã bỏ)

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
→ Kèm theo: `FamilyProjectController.cs`, `AdCampaignController.cs`, `Commands/FamilyProjects/`, `Commands/AdCampaigns/`, `Queries/AdCampaigns/`, job `AdBudgetDepletionJob.cs`, role `AdvertisingPartner`. (Đã xoá theo xác nhận trước đó — mục này giữ lại để đối chiếu, không cần làm lại.)

## 4. Entity cần THÊM MỚI vào Domain

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

## 5. Background Jobs — cập nhật

| Job | Trạng thái | Ghi chú |
|---|---|---|
| `SubscriptionExpiryJob` | Giữ nguyên | Đổi field cho khớp VIP mới, bỏ phần Family |
| `AdBudgetDepletionJob` | ❌ Đã xoá | — |
| `DailyQuestResetJob` | Cần thêm | 00:00 GMT+7 — chỉ cần nếu FT-27 vào scope 3 tuần |
| `ClanQuestResetJob` | Cần thêm | Thứ 2 00:00 — chỉ cần nếu FT-24 vào scope |
| `LeagueResetJob` | Cần thêm | Ngày 1 hàng tháng — chỉ cần nếu FT-24 vào scope |
| `ChallengeResultJob` | Cần thêm | CN 08:00 — chỉ cần nếu FT-23 vào scope |

## 6. Payment — đã chốt (không còn là vấn đề mở)

**2 giai đoạn**, xem chi tiết `MVP_SCOPE.md` và `CLAUDE.md` (BR-PAYMENT-01):
- **Giai đoạn 1 (đang code, trong FT-16):** xác nhận thủ công — Admin confirm tay qua `Transaction.ConfirmedByAdminId`.
- **Giai đoạn 2 (sau, không gấp):** SePay webhook — `Transaction.SePayReferenceCode` + bảng `SePayWebhookLog`, đã thiết kế sẵn trong ERD để không phải đổi schema khi tới lúc làm.

Khớp đúng với mô tả FE-04 trong `Report_1_Vision_Scope_v2.1.docx`.
