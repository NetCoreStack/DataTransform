using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public interface ITransformTask
    {
        List<DbTransformContext> DbTransformContexts { get; }

        Task InvokeAsync();
    }
}