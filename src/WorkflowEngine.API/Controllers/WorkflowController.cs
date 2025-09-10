using Microsoft.AspNetCore.Mvc;

namespace WorkflowEngine.API.Controllers;

public class WorkflowController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
