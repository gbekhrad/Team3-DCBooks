using Microsoft.AspNetCore.Mvc;
using Project498.WebApi.Services;

namespace Project498.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class StringsController(IStringService stringService) : ControllerBase
{
    [HttpGet("reverse/{input}")]
    public IActionResult Reverse(string input)
    {
        return Ok(stringService.Reverse(input));
    }
}
