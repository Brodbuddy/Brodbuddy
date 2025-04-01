using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmailController(IEmailSender sender) : ControllerBase
{
    [HttpPost]
    [Route("")]
    public async Task<IActionResult> SendFluentEmail()
    {
        await sender.SendEmailAsync("to", "topic", "content");
        return Ok();
    }
}