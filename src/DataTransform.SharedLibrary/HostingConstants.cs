using System.Threading;

namespace DataTransform.SharedLibrary
{
    public static class HostingConstants
    {
        public static readonly SemaphoreSlim SharedSemaphoreSlim = new SemaphoreSlim(1, 1);
    }
}
