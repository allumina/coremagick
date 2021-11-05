using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CoreMagick.Controllers
{
    [ApiController]
    [Route("api/v2/images")]
    public class ImagesController : ControllerBase
    {
        private const string CommandMagick = "/usr/local/bin/magick";
        private const string CommandArgsIdentify = "identify -format \"%m\"";
        private const string CommandArgsResize = "convert -resize";
        
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(ILogger<ImagesController> logger)
        {
            _logger = logger;
        }

        [HttpGet("hello")]
        public IActionResult Hello()
        {
            return Ok("Hello");
        }

        [HttpGet("encode")]
        public async Task<IActionResult> Encode(
            [FromQuery(Name = "source")] string source, 
            [FromQuery(Name = "resize")] bool resize = false,
            [FromQuery(Name = "width")] ushort width = 0,
            [FromQuery(Name = "force")] bool force = false)
        {
            try
            {
                var path = GenerateTemporaryPath();
                await RetrieveFile(source, path);
                var mime = Identify(path);
                if (resize && width > 0)
                {
                    Resize(path, width);
                }
                var output = Base64Encode(path);
                DeleteTemporaryFile(path);
                return Ok($"data:{mime};base64,{output}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        
        #region Private methods
        private async Task RetrieveFile(string url, string path)
        {
            using var client = new HttpClient();
            using var result = await client.GetAsync(url);
            if (result.IsSuccessStatusCode)
            {
                var binary = await result.Content.ReadAsByteArrayAsync(); 
                await System.IO.File.WriteAllBytesAsync(path, binary);
            }
        }

        private void DeleteTemporaryFile(string path)
        {
            System.IO.File.Delete(path);
        }
        
        private static string GenerateTemporaryPath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().ToLower());
        }
        
        private static string Base64Encode(string path) {
            return Convert.ToBase64String(System.IO.File.ReadAllBytes(path));
        }

        private string Identify(string path)
        {
            string output = RunCommand($"{CommandMagick}",  $"{CommandArgsIdentify} {path}");
            switch (output.Trim().ToLower())
            {
                case "jpg":
                    return "image/jpeg";
                case "jpeg":
                    return "image/jpeg";
                case "gif":
                    return "image/gif";
                case "png":
                    return "image/png";
                default:
                    throw new Exception("Unsupported image type");
            }
        }
        
        private static void Resize(string path, ushort width)
        {
            RunCommand($"{CommandMagick}",  $"{CommandArgsResize} {width}x {path} {path}");
        }
        
        private static string RunCommand(string command, string args)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
                
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrEmpty(error))
            {
                return output;
            }
            else
            {
                throw new Exception(error);
            }
        }
        #endregion
    }
}