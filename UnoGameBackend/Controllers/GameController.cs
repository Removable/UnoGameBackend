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

    [HttpGet("UsersInfo")]
    public IActionResult UsersInfo()
    {
        return Json(Player.Players);
    }

    [HttpGet("RoomInfo")]
    public IActionResult RoomInfo(int roomId)
    {
        return Json(Room.Rooms.FirstOrDefault(r => r.Id == roomId));
    }

    [HttpGet("GetRooms")]
    public IActionResult GetRooms()
    {
        var rooms = new int[] { 1, 2, 3, 4, 5 };
        return Json(rooms);
    }
}