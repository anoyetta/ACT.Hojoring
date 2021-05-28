namespace FFXIV.Framework.xivapi.Models
{
    public class ApiResultModel<T> where T : class, new()
    {
        public PaginationModel Pagination { get; set; }
        public T Results { get; set; }
    }

    public class PaginationModel
    {
        public int? Page { get; set; }
        public int? PageNext { get; set; }
        public int PageTotal { get; set; }
        public int Results { get; set; }
        public int ResultsPerPage { get; set; }
        public int ResultsTotal { get; set; }
    }
}
