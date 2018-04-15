using DataTransform.SharedLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NetCoreStack.WebSockets;
using System;
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
        private readonly IConnectionManager _connectionManager;

        public TransformController(IHostingEnvironment hostingEnvironment, IConnectionManager connectionManager)
        {
            _hostingEnvironment = hostingEnvironment;
            _connectionManager = connectionManager;
        }

        [HttpGet(nameof(FileTree))]
        public IActionResult FileTree()
        {
            var tree = new JsTreeDataModel
            {
                Text = "Files",
                Id = "#",
                Opened = "true",
                Type = "root"
            };

            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(_hostingEnvironment.WebRootPath, "configs"));
            PathUtility.WalkDirectoryTree(directoryInfo, tree);
            var list = new List<JsTreeDataModel> { tree };

            return Json(list);
        }
        
        [HttpGet(nameof(GetContent))]
        public async Task<IActionResult> GetContent([FromQuery] string filename)
        {
            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "configs", filename);
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var content = System.IO.File.ReadAllText(filePath);
                    return Json(new { data = content });
                }
                catch (Exception ex)
                {
                    await _connectionManager.WsErrorLog(ex);
                }
            }

            return NotFound();            
        }

        [HttpPost(nameof(SaveConfig))]
        public IActionResult SaveConfig([FromBody] SaveConfigModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "configs", model.ConfigFileName);
            System.IO.File.WriteAllText(filePath, model.Content);

            return Json(new { success = true });
        }

        [HttpGet(nameof(Download))]
        public IActionResult Download([FromQuery] string filename)
        {
            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "configs", filename);
            if (System.IO.File.Exists(filePath))
            {
                return File(System.IO.File.ReadAllBytes(filePath), "application/json", filename);
            }

            return NotFound();
        }

        [HttpPost(nameof(CreateTransformFile))]
        public IActionResult CreateTransformFile([FromBody] SaveFileModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            var filename = model.Filename;
            var extension = Path.GetExtension(filename);
            if (string.IsNullOrEmpty(extension))
            {
                filename = filename + ".json";
            }

            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "configs", filename);
            var content = string.Empty;

            // Rename file name
            if (!string.IsNullOrEmpty(model.OriginFilename) && filename != model.OriginFilename)
            {
                if (System.IO.File.Exists(filePath))
                {
                    return BadRequest("File exist");
                }

                var originFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "configs", model.OriginFilename);
                System.IO.File.Move(originFilePath, filePath);
                return Json(new { success = true });
            }

            if (System.IO.File.Exists(filePath))
            {
                // 
            }
            else
            {
                System.IO.File.Create(filePath);
            }

            return Json(new { success = true });
        }

        [HttpGet(nameof(StartTransformAsync))]
        public async Task<IActionResult> StartTransformAsync([FromQuery] string[] files)
        {
            if (files == null || !files.Any())
            {
                return BadRequest();
            }

            var transformManager = HttpContext.RequestServices.GetRequiredService<TransformManager>();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Factory.StartNew(async () => await transformManager.TransformAsync(files), TaskCreationOptions.LongRunning);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await Task.CompletedTask;

            return Json(new { success = true });
        }
    }
}
