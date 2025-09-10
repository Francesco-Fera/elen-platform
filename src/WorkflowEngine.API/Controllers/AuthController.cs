using Microsoft.AspNetCore.Mvc;

namespace WorkflowEngine.API.Controllers;

public class AuthController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
