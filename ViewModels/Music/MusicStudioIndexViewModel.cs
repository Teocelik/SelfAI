namespace SelfAI.ViewModels.Music;

/// <summary>
/// Music sekmesi (/Studio/Music) index view model (F.M.10c). Süre seçenekleri ve kapak
/// formatları statik — orchestrator whitelist'iyle birebir aynı olmalı.
/// </summary>
public class MusicStudioIndexViewModel
{
    public int[] BackingDurations { get; set; } = Array.Empty<int>();
    public int[] SongDurations { get; set; } = Array.Empty<int>();
    public CoverAspectOption[] CoverAspectRatios { get; set; } = Array.Empty<CoverAspectOption>();
}

public class CoverAspectOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
