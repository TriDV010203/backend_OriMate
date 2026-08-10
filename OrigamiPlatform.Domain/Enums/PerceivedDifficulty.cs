namespace OrigamiPlatform.Domain.Enums;

// A learner's own difficulty rating for a tutorial (Dễ/Trung bình/Khó), submitted once on first-time
// completion. Deliberately separate from TutorialDifficulty (author-set, at authoring time) — this is
// reader feedback collected after the fact.
public enum PerceivedDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}
