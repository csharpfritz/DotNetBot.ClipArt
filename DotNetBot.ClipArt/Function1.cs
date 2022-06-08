using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace DotNetBot.ClipArt
{
	public static class Function1
	{

		private static string _IndexHTML = String.Empty;

		static Function1()
		{

		}

		[FunctionName("Home")]
		public static IActionResult Home(
			[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "default")] HttpRequest req
		)
		{

			if (string.IsNullOrEmpty(_IndexHTML))
			{
				using var htmlStream = File.OpenRead("index.html");
				using var sr = new StreamReader(htmlStream);
				_IndexHTML = sr.ReadToEndAsync().GetAwaiter().GetResult();
			}

			return new ContentResult
			{
				Content = _IndexHTML,
				ContentType = "text/html"
			};

		}

		private static readonly string[] _ValidColors = new string[] { "yellow", "cyan", "blue", "white", "purple", "gray", "none", "random" };

		[FunctionName("Resize")]
		public static async Task<IActionResult> Run(
				[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{width:min(50)}/{height:max(3000)}/{color=None}")] HttpRequest req,
				int width,
				int height,
				string color,
				ILogger log)
		{

			// Check the background against valid list:
			if (!_ValidColors.Contains(color.ToLowerInvariant()))
				return new BadRequestObjectResult($"Invalid color requested.  Valid colors are: {string.Join(' ', _ValidColors)}");

			string responseMessage = $"Creating a bot of size {width}x{height} with {color} background";

			return new OkObjectResult(responseMessage);
			
		}
	}
}
