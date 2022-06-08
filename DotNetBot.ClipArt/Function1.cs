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
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Drawing;

namespace DotNetBot.ClipArt
{
	public static class Function1
	{

		private static string _IndexHTML = String.Empty;
		private static string[] _Images = new string[] {};

		[FunctionName("Home")]
		public static IActionResult Home(
			[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "default")] HttpRequest req,
			ExecutionContext context
		)
		{

			if (string.IsNullOrEmpty(_IndexHTML))
			{
				using var htmlStream = File.OpenRead(Path.Combine(context.FunctionAppDirectory, "index.html"));
				using var sr = new StreamReader(htmlStream);
				_IndexHTML = sr.ReadToEndAsync().GetAwaiter().GetResult();
			}

			// Cache the output
			var headers = req.HttpContext.Response.GetTypedHeaders();
			headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
			{
				MaxAge = TimeSpan.FromMinutes(5),
				NoStore = false,
				Public = true
			};

			return new ContentResult
			{
				Content = _IndexHTML,
				ContentType = "text/html"
			};

		}

		private static readonly Dictionary<string, System.Drawing.Color> _Colors = new Dictionary<string, System.Drawing.Color> {
			{ "blue", ColorTranslator.FromHtml("#A7CBF6") },
			{ "cyan", ColorTranslator.FromHtml("#C3F2F4") },
			{ "gray", ColorTranslator.FromHtml("#E5E5E1") },
			{ "purple", ColorTranslator.FromHtml("#DFD8F7") },
			{ "white", ColorTranslator.FromHtml("#FFF") },
			{ "yellow", ColorTranslator.FromHtml("#FFE589") },
			{ "random", System.Drawing.Color.White },
			{ "none", System.Drawing.Color.Transparent }
		};

		[FunctionName("Resize")]
		public static async Task<IActionResult> Run(
				[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{width:min(50)}/{height:max(3000)}/{color=random}")] HttpRequest req,
				int width,
				int height,
				string color,
				ExecutionContext context,
				ILogger log)
		{

			// Check the background against valid list:
			if (!_Colors.ContainsKey(color.ToLowerInvariant()))
				return new BadRequestObjectResult($"Invalid color requested.  Valid colors are: {string.Join(' ', _Colors.Keys)}");

			if (!_Images.Any()) IdentifyImages(context);

			log.LogInformation($"Creating a bot of size {width}x{height} with {color} background");

			var imageFilename = _Images.OrderBy(_ => Guid.NewGuid()).First();
			var encoder = new PngEncoder()
			{
				CompressionLevel = PngCompressionLevel.BestCompression,
				TransparentColorMode = PngTransparentColorMode.Clear,
				BitDepth = PngBitDepth.Bit8,
				ColorType = PngColorType.Palette
			};

			using var output = new MemoryStream();
			using var image = Image.Load(Path.Combine(context.FunctionAppDirectory, "images", imageFilename));

			var divisor = image.Width / (decimal)width;
			var targetHeight = Convert.ToInt32(Math.Round((decimal)(image.Height / divisor)));

			color = color.ToLowerInvariant();
			if (color == "random") color = _Colors.Take(_Colors.Count() - 2).OrderBy(_ => Guid.NewGuid()).First().Key;

			image.Mutate(x =>
			{
				x.Resize(new ResizeOptions() {
					Mode = ResizeMode.Pad,
					Size = new SixLabors.ImageSharp.Size(width, height),
				});
				if (!color.Equals("none", StringComparison.CurrentCultureIgnoreCase)) 
					x.BackgroundColor(_Colors[color].ToImageSharpColor());
			});
			image.Save(output, encoder);
			output.Position = 0;

			// Cache the output
			var headers = req.HttpContext.Response.GetTypedHeaders();
			headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
			{
				MaxAge = TimeSpan.FromMinutes(5),
				NoStore = false,
				Public = true
			};
			return new FileContentResult(output.ToArray(), "image/png");
			
		}

		private static void IdentifyImages(ExecutionContext context)
		{

			var dirInfo = new DirectoryInfo(Path.Combine(context.FunctionAppDirectory, "images"));
			_Images = dirInfo.EnumerateFiles()
				.Where(f => f.Extension == ".png")
				.Select(f => f.Name).ToArray();


		}
	}
}
