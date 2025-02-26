using InBoostTestApp.Controllers;
using InBoostTestApp.Data;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using InBoostUser = InBoostTestApp.Data.User;

namespace InBoostTestApp.Services
{
    /// <summary>
    /// Telegram interface
    /// </summary>
    public interface ITelegramBotService
    {
        /// <summary>
        /// Send message to user
        /// </summary>
        /// <param name="telegramId">Chat id</param>
        /// <param name="message">Text messag</param>
        Task SendMessage(long telegramId, string message);
    }

    /// <summary>
    /// Telegram implementation
    /// </summary>
    public class TelegramBotService : ITelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<HomeController> _logger;
        private readonly IDataService _context;
        private readonly IWeatherService _weatherService;

        const string WEATHER_COMMAND = "/weather ";
        const string BOT_TOKEN = "your_token";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="context">Data context service</param>
        /// <param name="weatherService">Weather service</param>
        public TelegramBotService(ILogger<HomeController> logger, IDataService context, IWeatherService weatherService)
        {
            _logger = logger;
            _context = context;
            _weatherService = weatherService;

            _botClient = new TelegramBotClient(BOT_TOKEN);
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates =
                [
                    UpdateType.Message
                ],
                DropPendingUpdates = false
            };

            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cts.Token);
        }

        private Task ErrorHandler(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            var msg = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(msg);
            return Task.CompletedTask;
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            var message = update.Message;
                            var user = message?.From ?? null;
                            var text = message?.Text ?? null;
                            if (user != null && text != null)
                            {
                                if (!text.StartsWith(WEATHER_COMMAND))
                                    return;
                                var city = text.Substring(WEATHER_COMMAND.Length);
                                if (string.IsNullOrWhiteSpace(city))
                                    return;

                                var weather = await _weatherService.GetWeather(city);
                                await SendMessage(message.Chat.Id, weather);

                                await _context.AddUserRequestAsync(new InBoostUser
                                {
                                    TelegramId = user.Id,
                                    Name = user.Username ?? user.LastName ?? user.FirstName ?? user.Id.ToString(),
                                }, new WeatherRequest { CityName = city }, DateTime.Now);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Send message to user
        /// </summary>
        /// <param name="telegramId">Chat id</param>
        /// <param name="message">Text messag</param>
        public async Task SendMessage(long telegramId, string message)
        {
            try
            {
                await _botClient.SendMessage(new ChatId(telegramId), message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
