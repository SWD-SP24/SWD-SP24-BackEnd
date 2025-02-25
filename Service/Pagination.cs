namespace SWD392.Service
{
    public class Pagination
    {
        public int LastVisiblePage { get; set; }
        public bool HasNextPage { get; set; }
        public int Total { get; set; }

        public Pagination(int lastVisiblePage, bool hasNextPage, int total = 0)
        {
            LastVisiblePage = lastVisiblePage;
            HasNextPage = hasNextPage;
            Total = total;
        }
    }
}
