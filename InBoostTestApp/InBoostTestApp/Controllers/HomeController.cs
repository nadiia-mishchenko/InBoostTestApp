using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InBoostTestApp.Models;
using InBoostTestApp.Services;

namespace InBoostTestApp.Controllers;

/// <summary>
/// Default controller
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDataService _context;
    private readonly ITelegramBotService _telegram;
    private readonly IWeatherService _weatherService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="context">Data context service</param>
    /// <param name="telegram">Telegram service</param>
    /// <param name="weatherService">Weather service</param>
    public HomeController(ILogger<HomeController> logger,
        IDataService context, ITelegramBotService telegram, IWeatherService weatherService)
    {
        _logger = logger;
        _context = context;
        _telegram = telegram;
        _weatherService = weatherService;
    }

    /// <summary>
    /// Default method
    /// </summary>
    /// <returns>Default view</returns>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Get user view
    /// </summary>
    /// <param name="userId">User id. If not set then all users are displayed</param>
    /// <returns>User(s) view</returns>
    [HttpGet]
    [Route("Home/Users/{userId?}")]
    [Route("users/{userId?}")]
    public async Task<IActionResult> Users(int? userId)
    {
        var user = userId.HasValue ? await _context.GetUserAsync(userId.Value) : null;
        if (user != null)
            return View("UserDetails", new UserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                WeatherRequests = [.. user.Requests.Select(item => new WeatherViewModel
                {
                    Id = item.Id,
                    City = item.CityName,
                    RequestDate = item.RequestDate
                })]
            });
        else
        {
            var users = await _context.GetUsersAsync();
            return View("Users", new List<UserViewModel>(users.Select(item => new UserViewModel
            {
                Id = item.Id,
                Name = item.Name,
                WeatherRequests = [.. item.Requests.Select(item => new WeatherViewModel
                {
                    Id = item.Id,
                    City = item.CityName,
                    RequestDate = item.RequestDate
                })]
            })));
        }
    }

    /// <summary>
    /// Send weather to user
    /// </summary>
    /// <param name="userId">User id. If not set then send to all users from the database</param>
    /// <returns>User view</returns>
    [HttpPost]
    [Route("SendWeather/{userId}")]
    [Route("sendWeatherToAll")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendWeather(int? userId = null)
    {
        var user = userId.HasValue ? await _context.GetUserAsync(userId.Value) : null;
        if (user != null)
        {
            if (user.Requests.Count > 0)
            {
                await _telegram.SendMessage(user.TelegramId, await _weatherService.GetWeather(user.GetLastRequest().CityName));
                await _context.AddUserRequestAsync(user, user.GetLastRequest(), DateTime.Now);
            }
        }
        else
        {
            var users = await _context.GetUsersAsync();
            var requests = users.Where(item => item.Requests.Count > 0).Select(item => item.GetLastRequest());
            foreach (var item in users.Where(item => item.Requests.Count > 0))
            {
                await _telegram.SendMessage(item.TelegramId, await _weatherService.GetWeather(item.GetLastRequest().CityName));
                await _context.AddUserRequestAsync(item, item.GetLastRequest(), DateTime.Now);
            }
            await _context.AddRequestsAsync(users.Where(item => item.Requests.Count > 0).Select(item => item.GetLastRequest()), DateTime.Now);
        }

        return await Users(userId);
    }

    /// <summary>
    /// Error
    /// </summary>
    /// <returns>Error view</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
