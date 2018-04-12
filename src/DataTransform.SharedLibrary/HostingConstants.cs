using System.Threading;

namespace DataTransform.SharedLibrary
{
    public static class HostingConstants
    {
        public static string TransformJsonFileFullPath { get; set; }

        public static readonly SemaphoreSlim SharedSemaphoreSlim = new SemaphoreSlim(1, 1);
    }
}
