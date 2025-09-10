using Microsoft.AspNetCore.Mvc;

namespace WorkflowEngine.API.Controllers;

public class UserController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
