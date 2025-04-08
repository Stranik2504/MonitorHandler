using Microsoft.AspNetCore.Mvc;

namespace MonitorHandler.Controllers;

[ApiController]
[Route("/api/v1/")]
public class MainController : Controller
{
    [HttpGet]
    public ActionResult<string> Get()
    {
        return "MonitorHandler API";
    }
}