using DataTransform.SharedLibrary;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public interface ITransformTask
    {
        Task InvokeAsync(DbTransformContext context);
    }
}