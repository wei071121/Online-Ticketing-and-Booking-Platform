using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QigloRestaurant.Web.Controllers;

[AllowAnonymous]
[Route("menu-photo")]
public class MenuPhotoController(IWebHostEnvironment environment) : Controller
{
    [HttpGet("{fileName}")]
    public IActionResult FileByName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, safeName, StringComparison.Ordinal))
            return BadRequest();

        var path = Path.Combine(environment.ContentRootPath, "App_Data", "MenuPhotos", safeName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        var contentType = Path.GetExtension(safeName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return PhysicalFile(path, contentType);
    }
}
