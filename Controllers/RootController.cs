using Microsoft.AspNetCore.Mvc;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Route("")]
public class RootController : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return Redirect("/swagger/index.html");
    }
}
