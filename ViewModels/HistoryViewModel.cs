using SelfAI.Entities;

namespace SelfAI.ViewModels
{
    public class HistoryViewModel
    {
        public IReadOnlyList<Generation> Generations { get; set; } = new List<Generation>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 12;

        // F.M.UI.3 — görsel/müzik tab filtresi
        public string CurrentType { get; set; } = "all";   // "all" | "image" | "music"
        public int ImageCount { get; set; }
        public int MusicCount { get; set; }
        public int AllCount => ImageCount + MusicCount;

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}
