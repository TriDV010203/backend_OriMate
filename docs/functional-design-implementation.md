# Functional Design & Implementation

**Version:** v3.0.0  
**Last Updated:** 2026-09-08  
**Scope:** FT-01 to FT-12 only  
**Source:** `OrigamiPlatform.API`, `OrigamiPlatform.Application`, `OrigamiPlatform.Domain`, `OrigamiPlatform.IntegrationTests`, and `OrigamiPlatform.Tests`

## Status convention

- `Done`: evidence exists in the backend source or a matching test class.
- `ToDo`: not verified in this review/run.
- `TBD`: project ownership or deadline is not represented in source code.
- `N/A`: not applicable to the backend implementation.

## Functional Design & Implementation Matrix

| ID | Name | Type | Main Actor | Module | Feature | UC-IDs | CRUD | Entity 1 | Entity 2 | Entity 3 | Plan | UCS Status | UCS In-charge | UCS Deadline | FDS Status | FDS In-charge | FDS Deadline | Code Status | Code In-charge | Code Deadline | IT-TC Status | IT-TC In-charge | IT-TC Deadline | IT-Ex Status | IT-Ex In-charge | IT-Ex Deadline | Notes |
|---|---|---|---|---|---|---|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FT-01 | Registration & Email Verification | Screen/API | Guest/User | Common | Auth | UC-01.1, UC-01.2, UC-01.3 | 2 | User | UserRole | EmailVerificationToken | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | `AuthController`: register, verify-email, resend-verification. |
| FT-02 | Login & Session Management | Screen/API | User | Common | Auth | UC-02.1, UC-02.2, UC-02.3, UC-02.4 | 2 | User | RefreshToken | UserSession | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | JWT login, refresh token, change password, logout. |
| FT-03 | Forgot / Reset Password | Screen/API | User | Common | Auth | UC-03.1, UC-03.2 | 2 | User | PasswordResetToken | Email | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | `forgot-password` and `reset-password` actions are implemented. |
| FT-04 | Library Search & Filter | Screen/API | Guest/User | Learning | Library | UC-04.1, UC-04.2, UC-04.3 | 1 | Tutorial | Category | User | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | Public tutorial list, slug detail, recommended list, and category filter. |
| FT-05 | Step-by-Step Learning | Screen/API | User | Learning | Tutorial Progress | UC-05.1, UC-05.2 | 2 | Tutorial | TutorialStep | TutorialProgress | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | `TutorialProgressController` records completion and stuck-thread events. |
| FT-06 | Server-Side VIP Content Lock | API/Business Rule | User | Monetization | VIP Access | UC-06.1, UC-06.2 | 1 | Tutorial | VipTier | UserSubscription | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | Access is enforced server-side; client visibility is not the authorization boundary. |
| FT-07 | Purchase VIP Package | Screen/API | User | Monetization | Subscription | UC-07.1, UC-07.2, UC-07.3 | 1 | UserSubscription | SubscriptionTransaction | VipTier | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | `SubscriptionController` supports subscribe, transaction lookup, and current subscriptions. |
| FT-08 | Achievement | Screen/API | User/Admin | Gamification | Achievement | UC-08.1, UC-08.2, UC-08.3 | 3 | Achievement | UserAchievement | Milestone | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | Achievement CRUD plus user achievements and milestones. |
| FT-09 | Draft & Submit Tutorial | Screen/API | Creator | Content | Tutorial Authoring | UC-09.1, UC-09.2, UC-09.3 | 2 | Tutorial | TutorialSection | TutorialVersion | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | Create, update, list own tutorials, and submit for review. |
| FT-10 | Edit Published Tutorial | Screen/API | Creator | Content | Tutorial Editing | UC-10.1, UC-10.2, UC-10.3 | 2 | Tutorial | TutorialWorkingCopy | TutorialVersion | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | Working-copy flow with submit-edit and manager approve/reject edit. |
| FT-11 | Tutorial Review | Screen/API | Manager/Admin | Content | Tutorial Moderation | UC-11.1, UC-11.2, UC-11.3, UC-11.4 | 2 | Tutorial | TutorialReview | User | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | Manager queue, publish, reject, admin list/detail, and remove actions. |
| FT-12 | User Management | Screen/API | Admin | Administration | User Management | UC-12.1, UC-12.2, UC-12.3 | 3 | User | UserRole | UserStatus | Iter1 | N/A | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | Done | TBD | TBD | ToDo | TBD | TBD | Admin user operations and role/status coverage are present in API and integration tests. |

## Functional design details

### FT-01 to FT-03: Authentication

- Base API: `/api/auth`.
- Registration creates the user flow and requires email verification before normal authenticated use.
- Login issues JWT credentials; refresh-token, logout, and password-change actions belong to session management.
- Forgot/reset password uses a tokenized flow and must not disclose whether an email is registered.
- Primary code evidence: `AuthController` and authentication handlers/validators under `OrigamiPlatform.Application`.
- Test evidence: `RegisterControllerTests`, `VerifyEmailControllerTests`, `LoginControllerTests`, `SessionControllerTests`, `PasswordAndSecurityControllerTests`, and `AuthWorkflowTests`.

### FT-04 to FT-08: Learning and monetization

- Public tutorial browsing is exposed by `/api/tutorials`; category and recommendation endpoints support the library screen.
- Progress completion and stuck reporting are exposed by `/api/tutorials/{tutorialId}/complete` and `/api/tutorials/{tutorialId}/steps/{stepId}/stuck`.
- Subscription operations are exposed by `/api/subscriptions`; shop pattern purchase is separate under `/api/shop`.
- Achievements are exposed by `/api/achievements`, including user achievements and milestones.
- Test evidence: `SearchAndFilterTests`, `TutorialProgressIntegrationTests`, `VipGatingTests`, `VipSubscriptionTests`, `AchievementTests`, and `AchievementAndMilestoneTests`.

### FT-09 to FT-12: Content and administration

- Tutorial authoring and review are implemented in `/api/tutorials` with creator, manager, and admin action boundaries.
- Draft submission uses `POST /api/tutorials` and `PUT /api/tutorials/{id}/submit`.
- Published tutorial editing uses working-copy endpoints: `POST /api/tutorials/{id}/edit`, `PUT /api/tutorials/{id}/edit-content`, and `PUT /api/tutorials/{id}/submit-edit`.
- Review uses manager queue, approve/reject, publish, and admin tutorial operations.
- User profile/admin management is covered by `UsersController`, `AdminController`, and the admin integration tests.
- Test evidence: `FT04_TutorialAuthoringTests`, `FT05_ManagerReviewTests`, `FT07_EditPublishedTutorialTests`, `TutorialAuthoringIntegrationTests`, `TutorialEditingIntegrationTests`, `TutorialManagerReviewIntegrationTests`, `AdminUsersControllerIntegrationTests`, and `AdminRoleManagementTests`.

## Verification note

`IT-TC Status` records the presence of matching test coverage. `IT-Ex Status` remains `ToDo` until the integration test suite is executed against the current checkout. The next verification command from the backend root is:

```powershell
dotnet test OrigamiPlatform.slnx
```
