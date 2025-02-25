namespace SWD392.Service
{
    public class Pagination
    {
        public int LastVisiblePage { get; set; }
        public bool HasNextPage { get; set; }

        public Pagination(int lastVisiblePage, bool hasNextPage)
        {
            LastVisiblePage = lastVisiblePage;
            HasNextPage = hasNextPage;
        }
    }
}
