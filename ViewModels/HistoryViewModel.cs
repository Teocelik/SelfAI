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

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}
