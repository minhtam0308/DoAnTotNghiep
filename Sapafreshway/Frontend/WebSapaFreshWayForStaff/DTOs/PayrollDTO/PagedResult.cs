using Newtonsoft.Json;

namespace SapaFreshWayForStaff.DTOs
{
    public class PagedResult<T>
    {
        [JsonProperty("totalRecords")]
        public int TotalCount { get; set; }

        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("data")]
        public List<T> Data { get; set; }
    }
}
