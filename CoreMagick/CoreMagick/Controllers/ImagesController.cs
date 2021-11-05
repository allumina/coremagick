using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        private const string CommandMagickIdentify = "/usr/bin/identify";
        private const string CommandMagickConvert = "/usr/bin/convert";
        private const string CommandArgsIdentify = "-format \"%m\"";
        private const string CommandArgsResize = "-resize";
        
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
                source = WebUtility.UrlDecode(source);
                _logger.LogInformation($"Retrieving {source}");
                await RetrieveFile(source, path);
                var mime = Identify(path);
                if (resize && width > 0)
                {
                    _logger.LogInformation($"Resizing (width {width}): {source}");
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
        
        [HttpGet("path")]
        public IActionResult GeneratePath()
        {
            var path = GenerateTemporaryPath();
            return Ok(path);
        }
        
        #region Private methods
        private async Task RetrieveFile(string url, string path)
        {
            using var client = new HttpClient();
            using var result = await client.GetAsync(url);
            _logger.LogInformation($"Status {result.StatusCode.ToString()}");
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
            string output = RunCommand($"{CommandMagickIdentify}",  $"{CommandArgsIdentify} {path}");
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
            RunCommand($"{CommandMagickConvert}",  $"{CommandArgsResize} {width}x {path} {path}");
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