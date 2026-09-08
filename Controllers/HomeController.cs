using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // Required only to display the requested static About Us page.
    // It has no database access and does not change system behaviour.
    public IActionResult About()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
