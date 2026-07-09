# OriMate — ERD Design v3

Origami Community Platform — Database Schema (theo BRD v5.0 & `docs/MVP_SCOPE.md`)

> Phạm vi: chỉ liệt kê bảng thuộc **MUST-HAVE** và **SHOULD-HAVE** của đợt code 3 tuần. Bảng thuộc **WON'T-HAVE** (Future Work) được liệt kê tên ở cuối tài liệu, KHÔNG thiết kế chi tiết — tránh tạo migration cho phần chưa được duyệt.

Tham chiếu: `docs/FT_MAPPING_v5.md` · `docs/MVP_SCOPE.md` · `docs/CLAUDE.md`

---

## Quy ước chung

- Mọi PK là Guid, `ValueGeneratedNever()` — generate ở Application layer, không dùng DB identity.
- Mọi bảng có `CreatedAt` (datetime2, UTC); bảng có thể sửa thêm `UpdatedAt`.
- Mọi string field đều có `HasMaxLength` — không để `nvarchar(max)` ngoài ý muốn, trừ `TutorialWorkingCopy.StepsSnapshotJson`.
- Bảng đánh dấu ⚠️ IMMUTABLE (`TutorialReviewHistory`, `AuditLog`): chỉ cấu hình INSERT, cấm `Update()`/`Remove()` ở Repository.
- FK dùng convention: `[EntityName]Id`, trỏ vào bảng cùng tên số ít.

---

## 1. Auth & User *(FT-01, FT-02, FT-03)*

### User

*Tài khoản gốc.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK | Generated ở application layer |
| Email | nvarchar(256) | Unique, NOT NULL | BR-AUTH-01 |
| PasswordHash | nvarchar(256) | NOT NULL | BCrypt |
| DisplayName | nvarchar(100) | NOT NULL |  |
| AccountStatus | enum | NOT NULL | Unverified / Active / Suspended |
| CreatedAt | datetime2 | NOT NULL |  |
| UpdatedAt | datetime2 | NULL |  |

### UserRole

*Vai trò được Admin bổ nhiệm (User/Creator/CTV/Manager/Admin). Guest = chưa đăng nhập, không lưu bảng.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User, Unique |  |
| Role | enum | NOT NULL | User / Creator / ContributorReviewer / Manager / Admin |
| AssignedAt | datetime2 | NOT NULL |  |

### UserProfile

*Thông tin mở rộng + Skill Level (derived).*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| UserId | Guid | PK, FK → User |  |
| AvatarUrl | nvarchar(500) | NULL | Cloudinary |
| Bio | nvarchar(500) | NULL |  |
| SkillPoints | int | NOT NULL, default 0 | BR-SKILL-01 — cập nhật khi Tutorial Completed |
| SkillLevel | enum | NOT NULL, default Beginner | Beginner 0-4 / Intermediate 5-19 / Advanced ≥20 |

### RefreshToken

*Refresh token rotation (lưu DB).*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| TokenHash | nvarchar(256) | NOT NULL | Không lưu plaintext |
| ExpiresAt | datetime2 | NOT NULL |  |
| IsRevoked | bit | NOT NULL, default 0 | Set true khi đổi mật khẩu (BR-AUTH-02) |
| CreatedAt | datetime2 | NOT NULL |  |

### EmailLog

*Token verify email / reset password.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| Type | enum | NOT NULL | Verify (24h) / Reset (1h) |
| TokenHash | nvarchar(256) | NOT NULL |  |
| ExpiresAt | datetime2 | NOT NULL |  |
| UsedAt | datetime2 | NULL | Single-use — resend huỷ token cũ |

## 2. Tutorial Lifecycle *(FT-04 → FT-08, FT-11)*

### Category

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| Name | nvarchar(100) | Unique, NOT NULL |  |
| Slug | nvarchar(120) | Unique, NOT NULL |  |
| IsActive | bit | NOT NULL, default 1 | Quản lý bởi Admin — FT-03 |

### BlockedWord

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| Word | nvarchar(100) | Unique, NOT NULL |  |
| IsActive | bit | NOT NULL, default 1 | Cache in-memory HashSet — IBlockedWordService |

### Tutorial

*Entity trung tâm.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| AuthorId | Guid | FK → User | NULL nếu IsOfficial = true (BR-PATH-01) |
| Title | nvarchar(150) | NOT NULL | 5-150 ký tự — BR-TUT-* |
| Description | nvarchar(500) | NOT NULL | 20-500 ký tự |
| CategoryId | Guid | FK → Category |  |
| Difficulty | enum | NOT NULL | Dễ / Trung bình / Khó |
| CoverImageUrl | nvarchar(500) | NOT NULL | Cloudinary |
| Tags | nvarchar(300) | NULL | CSV hoặc bảng phụ nếu cần filter sâu |
| Slug | nvarchar(200) | Unique, NOT NULL | Giữ nguyên qua các lần Edit (BR-TUT-04) |
| Status | enum | NOT NULL | Draft / PendingManagerReview / Published / RejectedNeedChanges / Removed |
| IsVip | bit | NOT NULL, default 0 | BR-VIP-03: cần CreatorVipSettings active trước |
| IsOfficial | bit | NOT NULL, default 0 | FT-32 — seed content, không tính doanh thu |
| CreatedAt | datetime2 | NOT NULL |  |
| UpdatedAt | datetime2 | NULL |  |

### TutorialStep

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| TutorialId | Guid | FK → Tutorial |  |
| StepOrder | int | NOT NULL | 1..30 |
| ImageUrl | nvarchar(500) | NULL | ≥1 trong 3 loại bắt buộc |
| YoutubeUrl | nvarchar(300) | NULL |  |
| TextContent | nvarchar(1000) | NULL |  |

### TutorialWorkingCopy

*Bản nháp khi Edit sau Publish — bản gốc vẫn public cho đến khi Manager ApproveEdit.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| TutorialId | Guid | FK → Tutorial (bản gốc) |  |
| Title | nvarchar(150) | NOT NULL |  |
| Description | nvarchar(500) | NOT NULL |  |
| StepsSnapshotJson | nvarchar(max) | NOT NULL | Snapshot toàn bộ step mới — swap khi được duyệt |
| Status | enum | NOT NULL | PendingManagerReview / Approved / Rejected |
| CreatedAt | datetime2 | NOT NULL |  |

### TutorialReviewHistory

*⚠️ IMMUTABLE — chỉ INSERT, không UPDATE/DELETE.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| TutorialId | Guid | FK → Tutorial |  |
| ReviewerId | Guid | FK → User (Manager) | 1 vòng duy nhất — CTV không tham gia |
| Action | enum | NOT NULL | Publish / RejectNeedChanges / Remove |
| Reason | nvarchar(500) | NULL khi Publish, ≥10 ký tự khi Reject/Remove |  |
| CreatedAt | datetime2 | NOT NULL |  |

### TutorialVariant

*Should-have — FT-11.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| ParentTutorialId | Guid | FK → Tutorial |  |
| VariantTutorialId | Guid | FK → Tutorial | Tutorial độc lập, đánh dấu là biến thể |
| DifficultyDelta | int | NULL | Dùng cho gợi ý 'Gấp tiếp theo' |

## 3. Learning *(FT-09, FT-10)*

### StepProgress

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| TutorialId | Guid | FK → Tutorial |  |
| CompletedStepsCount | int | NOT NULL, default 0 |  |
| Status | enum | NOT NULL | InProgress / Completed / Archived — BR-TUT-04 |
| UpdatedAt | datetime2 | NOT NULL | Dùng cho Re-engagement 48h & Discovery 1-99% |

### StuckThread

*Nút Stuck tại 1 bước — không tính điểm, không thưởng Hạt.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| TutorialId | Guid | FK → Tutorial |  |
| StepId | Guid | FK → TutorialStep |  |
| UserId | Guid | FK → User |  |
| CreatedAt | datetime2 | NOT NULL | Reply dùng chung bảng Comment, TargetType = StuckThread |

## 4. Community *(FT-12 → FT-15)*

### CommunityPost

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| Content | nvarchar(1000) | NOT NULL | 1-1000 ký tự, blocked word check |
| TutorialId | Guid | FK → Tutorial, NULL | Gắn tag tutorial (tuỳ chọn) |
| CreatedAt | datetime2 | NOT NULL |  |

### CommunityPostMedia

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| PostId | Guid | FK → CommunityPost |  |
| MediaUrl | nvarchar(500) | NOT NULL | ≤10 media/post |
| MediaType | enum | NOT NULL | Image / Video |

### Comment

*Dùng chung cho Post và StuckThread.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| TargetType | enum | NOT NULL | Post / StuckThread |
| TargetId | Guid | NOT NULL |  |
| UserId | Guid | FK → User |  |
| Content | nvarchar(500) | NOT NULL | ≤500 ký tự, blocked word check |
| CreatedAt | datetime2 | NOT NULL |  |

### Like

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| TargetType | enum | NOT NULL | Post / Comment |
| TargetId | Guid | NOT NULL |  |
| UserId | Guid | FK → User |  |
| CreatedAt | datetime2 | NOT NULL | Unique (TargetType, TargetId, UserId) |

### FollowRelationship

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| FollowerId | Guid | FK → User |  |
| FollowingId | Guid | FK → User | Unique (FollowerId, FollowingId) |
| CreatedAt | datetime2 | NOT NULL |  |

### Report

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| TargetType | enum | NOT NULL | Post / Comment / Tutorial / User |
| TargetId | Guid | NOT NULL |  |
| ReporterId | Guid | FK → User |  |
| Reason | nvarchar(500) | NOT NULL |  |
| Status | enum | NOT NULL | Pending / Resolved |
| CreatedAt | datetime2 | NOT NULL |  |

### AuditLog

*⚠️ IMMUTABLE.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| ActorId | Guid | FK → User |  |
| Action | nvarchar(100) | NOT NULL |  |
| TargetType | nvarchar(50) | NOT NULL |  |
| TargetId | Guid | NOT NULL |  |
| CreatedAt | datetime2 | NOT NULL |  |

### Notification

*Should-have — đơn giản hoá, không real-time push trong 3 tuần.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| Type | nvarchar(50) | NOT NULL |  |
| Content | nvarchar(300) | NOT NULL |  |
| IsRead | bit | NOT NULL, default 0 |  |
| CreatedAt | datetime2 | NOT NULL |  |

### Wishlist

*Should-have.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| TutorialId | Guid | FK → Tutorial | Unique (UserId, TutorialId) |
| CreatedAt | datetime2 | NOT NULL |  |

## 5. Monetisation (thanh toán thủ công) *(FT-16, FT-17)*

### CreatorVipSettings

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| CreatorId | Guid | FK → User, Unique |  |
| TierName | nvarchar(100) | NOT NULL |  |
| Description | nvarchar(500) | NULL |  |
| IsActive | bit | NOT NULL, default 0 | BR-VIP-03 |

### VipSubscription

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| CreatorId | Guid | FK → User |  |
| StartDate | datetime2 | NOT NULL |  |
| EndDate | datetime2 | NOT NULL | 30 ngày cố định — BR-VIP-02 |
| Status | enum | NOT NULL | Active / Expired — SubscriptionExpiryJob |
| CreatedAt | datetime2 | NOT NULL |  |

### Transaction

*Thiết kế chấp nhận CẢ 2 nguồn xác nhận — Giai đoạn 1 (3 tuần, đang code): chỉ dùng nhánh thủ công (ConfirmedByAdminId). Giai đoạn 2 (sau, không gấp): thêm nhánh SePay (SePayReferenceCode) mà KHÔNG cần đổi schema.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| Amount | decimal(18,2) | NOT NULL |  |
| Status | enum | NOT NULL | PendingPayment / Confirmed / Rejected / Expired |
| ConfirmedByAdminId | Guid | FK → User, NULL | Giai đoạn 1 — Admin xác nhận tay |
| Note | nvarchar(300) | NULL | Giai đoạn 1 — vd: mã giao dịch chuyển khoản người dùng ghi |
| SePayReferenceCode | nvarchar(100) | Unique, NULL | Giai đoạn 2 — chỉ có giá trị khi dùng nhánh SePay |
| ConfirmedAt | datetime2 | NULL | Set khi 1 trong 2 nhánh xác nhận thành công |
| CreatedAt | datetime2 | NOT NULL |  |

### SePayWebhookLog

*⚠️ GIAI ĐOẠN 2 — chưa cần tạo bảng này trong 3 tuần đầu. Chỉ tạo khi bắt đầu tích hợp SePay thật. Immutable, dùng chống xử lý webhook trùng (idempotency) và audit tranh chấp giao dịch.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| TransactionId | Guid | FK → Transaction, NULL nếu không khớp giao dịch nào |  |
| SePayReferenceCode | nvarchar(100) | NOT NULL | Dùng để check đã xử lý webhook này chưa |
| SignatureVerified | bit | NOT NULL | false thì KHÔNG được xử lý payload |
| RawPayload | nvarchar(max) | NOT NULL | Lưu nguyên payload để audit/debug |
| ProcessedAt | datetime2 | NULL | NULL nếu nhận nhưng chưa xử lý (ví dụ do trùng) |
| CreatedAt | datetime2 | NOT NULL |  |

## 6. Achievement & Skill *(FT-19, FT-25)*

### Achievement

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| TutorialId | Guid | FK → Tutorial | Unique (UserId, TutorialId) — BR-33 |
| ImageUrl | nvarchar(500) | NOT NULL | ≤10MB |
| Note | nvarchar(500) | NULL |  |
| IsPublic | bit | NOT NULL, default 0 | BR-PORTFOLIO-01 |
| CreatedAt | datetime2 | NOT NULL |  |

## 7. Clan (Should-have, rút gọn) *(FT-22)*

### Clan

*Should-have — chỉ create/join/invite/leave, KHÔNG làm Level/slot/League trong 3 tuần.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| Name | nvarchar(100) | Unique, NOT NULL |  |
| OwnerId | Guid | FK → User |  |
| CreatedAt | datetime2 | NOT NULL |  |

### ClanMember

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| ClanId | Guid | FK → Clan |  |
| UserId | Guid | FK → User, Unique | BR-CLAN-01: 1 user 1 clan |
| JoinedAt | datetime2 | NOT NULL |  |
| ContributionPoints | int | NOT NULL, default 0 | Để dành cho Level/League ở bản sau |

### ClanInvite

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| ClanId | Guid | FK → Clan |  |
| UserId | Guid | FK → User |  |
| Status | enum | NOT NULL | Pending / Accepted / Expired |
| ExpiresAt | datetime2 | NOT NULL | 48h |
| CreatedAt | datetime2 | NOT NULL |  |

## 8. Gamification: Quest / Streak / Hạt Gấp (Should-have, rút gọn) *(FT-26, FT-27, FT-28)*

### DailyQuest

*Should-have — CHỈ 1 pool trong 3 tuần (bỏ tách Normal/Chill).*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| Title | nvarchar(200) | NOT NULL |  |
| TargetValue | int | NOT NULL |  |
| IsActive | bit | NOT NULL, default 1 |  |

### UserDailyQuestProgress

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| QuestId | Guid | FK → DailyQuest |  |
| QuestDate | date | NOT NULL | Unique (UserId, QuestId, QuestDate) — reset 00:00 GMT+7 |
| Progress | int | NOT NULL, default 0 |  |
| IsCompleted | bit | NOT NULL, default 0 |  |

### StreakLog

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User, Unique |  |
| CurrentStreak | int | NOT NULL, default 0 |  |
| LongestStreak | int | NOT NULL, default 0 |  |
| LastActiveDate | date | NULL |  |
| FreezeCount | int | NOT NULL, default 0 | Tối đa 2 — BR-SEEDS-04 |

### HatGapTransaction

*Sổ cái Hạt Gấp — CHỈ hỗ trợ sink Streak Freeze trong 3 tuần.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| UserId | Guid | FK → User |  |
| Amount | int | NOT NULL | Dương = earn, âm = spend |
| Type | enum | NOT NULL | Earn / Spend |
| Source | nvarchar(100) | NOT NULL | Ví dụ: TutorialComplete, DailyQuestBonus, StreakFreezePurchase |
| BalanceAfter | int | NOT NULL |  |
| CreatedAt | datetime2 | NOT NULL |  |

## 9. Shop (Should-have) *(FT-18)*

### ShopLink

*Should-have — rất nhẹ, chỉ link affiliate ngoài.*

| Field | Type | Constraint | Ghi chú |
|---|---|---|---|
| Id | Guid | PK |  |
| Title | nvarchar(200) | NOT NULL |  |
| Url | nvarchar(500) | NOT NULL |  |
| ImageUrl | nvarchar(500) | NULL |  |
| Category | nvarchar(100) | NULL | Ví dụ: Giấy, Kit, Sách |
| IsActive | bit | NOT NULL, default 1 | Quản lý bởi Admin |

---

## Quan hệ chính (FK quan trọng cần chú ý)

- `Tutorial.AuthorId` → `User.Id` (nullable khi `IsOfficial = true`, BR-PATH-01)
- `TutorialReviewHistory.TutorialId` → `Tutorial.Id` (1-n, immutable, không xoá khi Tutorial bị Remove)
- `StepProgress` (UserId, TutorialId) unique — 1 user chỉ có 1 progress record / tutorial
- `Achievement` (UserId, TutorialId) unique — BR-33, 1 achievement / user / tutorial
- `ClanMember.UserId` unique toàn bảng — BR-CLAN-01, 1 user chỉ ở 1 Clan
- `VipSubscription` (UserId, CreatorId) — không unique, cho phép gia hạn (`SubscriptionExpiryJob` set Expired trước khi tạo record mới)
- `HatGapTransaction.BalanceAfter` là số dư SAU giao dịch — dùng để audit, không tính lại bằng `SUM()` mỗi lần đọc

---

## Bảng đã XOÁ khỏi thiết kế (scope cũ, đã bị Hội đồng yêu cầu bỏ)

| Bảng | Lý do |
|---|---|
| FamilySubscription / FamilyProject / FamilyProjectMember / FamilyProjectStepProgress | Family Plan đã thay thế hoàn toàn bằng Clan |
| AdPlacement / AdCampaign / AdBanner / AdImpression / AdClick | Hệ quảng cáo đã bỏ hoàn toàn, thay bằng ShopLink |

---

## Bảng dự kiến TƯƠNG LAI (WON'T-HAVE — chưa thiết kế chi tiết, chưa tạo migration)

*Chỉ liệt kê tên để tránh trùng khi đặt tên bảng mới. Thiết kế chi tiết sẽ làm ở version ERD tiếp theo, sau khi MVP 3 tuần hoàn tất và được duyệt mở rộng scope.*

| Bảng dự kiến | Thuộc FT |
|---|---|
| WeeklyChallenge, ChallengeSubmission, ChallengeVote | FT-23 — Weekly Challenge & pairwise voting |
| ClanQuest, LeaguePoints | FT-24 — Clan Quest & League |
| PersonalMilestone | FT-20 |
| Journal (Folding Journal) | FT-21 |
| PaperPattern, SeasonalCosmetic, PrestigeItem | FT-28 (phần mở rộng Hạt Gấp — cosmetic/prestige) |
| LearningPath, LearningPathItem | FT-33 |
