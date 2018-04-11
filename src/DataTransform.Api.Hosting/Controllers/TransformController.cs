using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace DataTransform.Api.Hosting.Controllers
{
    [Route("api/[controller]")]
    public class TransformController : Controller
    {
        private readonly IOptionsSnapshot<TransformOptions> _options;

        public TransformController(IOptionsSnapshot<TransformOptions> options)
        {
            _options = options;
        }

        [HttpGet(nameof(GetOptions))]
        public IActionResult GetOptions()
        {
            List<TransformDescriptor> maps = _options.Value.Maps.ToList();
            return Json(maps);
        }
    }
}
