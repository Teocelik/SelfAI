namespace SelfAI.Services.Generation.Providers.FalAi.Models;

/// <summary>
/// fal.ai request durumlarının tip-güvenli karşılığı.
/// Ham string'ler (IN_QUEUE vb.) yerine kod içinde bu enum kullanılır.
/// </summary>
public enum FalAiRequestStatus
{
    Unknown = 0,
    InQueue = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}

public static class FalAiRequestStatusExtensions
{
    /// <summary>fal.ai'dan gelen ham status string'ini enum'a çevirir (case-insensitive).</summary>
    public static FalAiRequestStatus ParseStatus(string statusString)
    {
        return statusString?.ToUpperInvariant() switch
        {
            "IN_QUEUE" => FalAiRequestStatus.InQueue,
            "IN_PROGRESS" => FalAiRequestStatus.InProgress,
            "COMPLETED" => FalAiRequestStatus.Completed,
            "FAILED" => FalAiRequestStatus.Failed,
            _ => FalAiRequestStatus.Unknown
        };
    }

    /// <summary>Terminal durum mu? (Completed veya Failed — polling sonlanır.)</summary>
    public static bool IsTerminal(this FalAiRequestStatus status)
    {
        return status == FalAiRequestStatus.Completed || status == FalAiRequestStatus.Failed;
    }
}
