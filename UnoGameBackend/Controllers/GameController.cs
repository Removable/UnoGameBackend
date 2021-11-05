using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using UnoGameBackend.Data;
using UnoGameBackend.Hubs;

namespace UnoGameBackend.Controllers;

[Route("[controller]")]
public class GameController : Controller
{
    private readonly IHubContext<UnoHub> _hubContext;

    public GameController(IHubContext<UnoHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpGet("test")]
    public void Test()
    {
        var a = 1 + 1;
    }
}