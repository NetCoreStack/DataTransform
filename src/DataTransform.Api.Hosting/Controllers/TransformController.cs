using DataTransform.SharedLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting.Controllers
{
    [Route("api/[controller]")]
    public class TransformController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IOptionsSnapshot<TransformOptions> _options;

        public TransformController(IHostingEnvironment hostingEnvironment, IOptionsSnapshot<TransformOptions> options)
        {
            _hostingEnvironment = hostingEnvironment;
            _options = options;
        }

        [HttpGet(nameof(FileTree))]
        public IActionResult FileTree()
        {
            var tree = new JsTreeDataModel
            {
                Text = "Files",
                Id = "Files",
                Opened = "true",
                Type = "root"
            };

            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(_hostingEnvironment.WebRootPath, "configs"));
            PathUtility.WalkDirectoryTree(directoryInfo, tree);
            var list = new List<JsTreeDataModel> { tree };

            return Json(list);
        }
        
        [HttpGet(nameof(GetContent))]
        public IActionResult GetContent([FromQuery] string filename)
        {
            List<TransformDescriptor> maps = _options.Value.Maps.ToList();

            if (filename == "transform.json")
            {
                var content = System.IO.File.ReadAllText(HostingConstants.TransformJsonFileFullPath);
                return Json(new { data = content });
            }

            return NotFound();            
        }

        [HttpPost(nameof(SaveConfig))]
        public IActionResult SaveConfig([FromBody] SaveConfigModel model)
        {
            if (model.ConfigFileName == "transform.json")
            {
                System.IO.File.WriteAllText(HostingConstants.TransformJsonFileFullPath, model.Content);
            }            

            return Json(new { success = true });
        }

        [HttpGet(nameof(Download))]
        public IActionResult Download()
        {
            return File(System.IO.File.ReadAllBytes(HostingConstants.TransformJsonFileFullPath), "application/json", "transform.json");
        }

        [HttpPost(nameof(SaveTransformFile))]
        public IActionResult SaveTransformFile([FromBody] SaveFileModel model)
        {

            return Json(new { success = true });
        }

        [HttpGet(nameof(GetOptions))]
        public IActionResult GetOptions()
        {
            List<TransformDescriptor> maps = _options.Value.Maps.ToList();
            return Json(maps);
        }

        [HttpGet(nameof(StartTransformAsync))]
        public async Task<IActionResult> StartTransformAsync()
        {
            var transformManager = HttpContext.RequestServices.GetService<TransformManager>();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Factory.StartNew(async () => await transformManager.TransformAsync(), TaskCreationOptions.LongRunning);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await Task.CompletedTask;

            return Json(new { success = true });
        }
    }
}
