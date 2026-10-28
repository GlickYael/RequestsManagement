namespace RequestsManagement.DTOs
{
    public class RequestsPageResultDto : PageResultDto<RequestDto>
    {
       public Dictionary<string, int> StatusCounts { get; set; } = new();

        public Dictionary<string, int> PriorityCounts { get; set; } = new();
    }
}