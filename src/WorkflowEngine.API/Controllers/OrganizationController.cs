using Microsoft.AspNetCore.Mvc;

namespace WorkflowEngine.API.Controllers;

public class OrganizationController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
