using Dapper;
using InBoostTestApp.Data;
using Microsoft.Data.SqlClient;

namespace InBoostTestApp.Services
{
    /// <summary>
    /// Data intereface
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Get user from the database. User should be present in the database.
        /// For testing purposes it's enough, but in real application we should check user presence
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>User data</returns>
        public Task<User> GetUserAsync(int userId);

        /// <summary>
        /// Get all users from the database
        /// </summary>
        /// <returns>List of all users with their requests</returns>
        public Task<IEnumerable<User>> GetUsersAsync();

        /// <summary>
        /// Add new user weather request to the database. User can be new, city can be new.
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="request">Request with city name</param>
        /// <param name="requestDate">Request date</param>
        public Task AddUserRequestAsync(User user, WeatherRequest request, DateTime requestDate);

        /// <summary>
        /// Add requests for users. User cannot be new, cities cannot be new.
        /// Application sends requests for existing users only according to their last city requests, so we assume there is no new user/city.
        /// It's enough for testing purposes, but should be modified in real application.
        /// </summary>
        /// <param name="requests">Requests</param>
        /// <param name="requestDate">Requests date</param>
        public Task AddRequestsAsync(IEnumerable<WeatherRequest> requests, DateTime requestDate);
    }

    /// <summary>
    /// Data implementation
    /// </summary>
    public class DataService : IDataService
    {
        #region Constants

        const string DATABASE_SERVERNAME = "(LocalDB)\\MSSQLLocalDB";
        const string DATABASE_FILENAME = "InBoostTestDB.mdf";
        const string SELECT_USER = @"SELECT TOP (1) [Id], [TelegramId], [Name] FROM [dbo].[Users] WHERE [dbo].[Users].[Id] = @UserId";
        const string SELECT_ALL_USERS = @"SELECT [Id], [TelegramId], [Name] FROM [dbo].[Users]";
        const string SELECT_REQUESTS = @"SELECT [dbo].[WeatherHistory].[Id], [dbo].[WeatherHistory].[UserId], [dbo].[WeatherHistory].[CityId],
                            [dbo].[Cities].[Name] AS [CityName], [dbo].[WeatherHistory].[RequestDate]
                            FROM [dbo].[WeatherHistory] INNER JOIN [dbo].[Cities] ON [dbo].[WeatherHistory].[CityId] = [dbo].[Cities].[Id]
                            WHERE [dbo].[WeatherHistory].[UserId] = @UserId";
        const string SELECT_USER_BY_TELEGRAM_ID = "SELECT TOP (1) [Id], [Name] FROM [dbo].[Users] WHERE [TelegramId] = @TelegramId";
        const string SELECT_CITY_ID = "SELECT TOP (1) [Id] FROM [dbo].[Cities] WHERE [Name] = @Name";
        const string ADD_USER = "IF NOT EXISTS (SELECT [Id] FROM [dbo].[Users] WHERE [TelegramId] = @TelegramId) INSERT INTO [dbo].[Users] ([TelegramId], [Name]) VALUES (@TelegramId, @Name)";
        const string ADD_CITY = "IF NOT EXISTS (SELECT [Id] FROM [dbo].[Cities] WHERE [Name] = @Name) INSERT INTO [dbo].[Cities] ([Name]) VALUES (@Name)";
        const string ADD_REQUEST = "INSERT INTO [dbo].[WeatherHistory]([UserId], [CityId], [RequestDate]) VALUES (@UserId, @CityId, @RequestDate)";
        const string UPDATE_USER = "UPDATE [dbo].[Users] SET [Name] = @Name WHERE [Id] = @Id";

        #endregion Constants

        private readonly IConfiguration Configuration;
        private readonly ILogger<DataService> Logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">Logger</param>
        public DataService(IConfiguration configuration, ILogger<DataService> logger)
        {
            Configuration = configuration;
            Logger = logger;
        }

        /// <summary>
        /// Get user from the database. User should be present in the database.
        /// For testing purposes it's enough, but in real application we should check user presence
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>User data</returns>
        public async Task<User> GetUserAsync(int userId)
        {
            User user;
            using (var connection = new SqlConnection(GetDBConnectionString()))
            {
                await connection.OpenAsync();
                using (var result = await connection.QueryMultipleAsync($"{SELECT_USER};{SELECT_REQUESTS}", new { UserId = userId }))
                {
                    user = result.ReadFirst<User>();
                    user.Requests.AddRange(result.Read<WeatherRequest>());
                }
            }
            return user;
        }

        /// <summary>
        /// Get all users from the database
        /// </summary>
        /// <returns>List of all users with their requests</returns>
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            var result = new List<User>();
            using (var connection = new SqlConnection(GetDBConnectionString()))
            {
                await connection.OpenAsync();
                result.AddRange(await connection.QueryAsync<User>(SELECT_ALL_USERS));
                foreach (var user in result)
                {
                    user.Requests.AddRange(await connection.QueryAsync<WeatherRequest>(SELECT_REQUESTS, new { UserId = user.Id }));
                }
            }
            return result;
        }

        /// <summary>
        /// Add new user weather request to the database. User can be new, city can be new.
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="request">Request with city name</param>
        /// <param name="requestDate">Request date</param>
        public async Task AddUserRequestAsync(User user, WeatherRequest request, DateTime requestDate)
        {
            using (var connection = new SqlConnection(GetDBConnectionString()))
            {
                await connection.OpenAsync();
                using (var tr = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        if (user.Id == 0)
                        {
                            await connection.ExecuteAsync(ADD_USER,
                                new { user.TelegramId, user.Name },
                                tr);
                        }
                        var dbUser = await connection.QuerySingleAsync<User>(SELECT_USER_BY_TELEGRAM_ID, new { user.TelegramId }, tr);
                        if (user.Name != dbUser.Name)
                            await connection.ExecuteAsync(UPDATE_USER,
                                new { dbUser.Id, user.Name },
                                tr);

                        if (request.CityId == 0)
                        {
                            await connection.ExecuteAsync(ADD_CITY,
                                new { Name = request.CityName },
                                tr);
                            request.CityId = await connection.QuerySingleAsync<int>(SELECT_CITY_ID, new { Name = request.CityName }, tr);
                        }
                        await connection.ExecuteAsync(ADD_REQUEST,
                            new { UserId = dbUser.Id, request.CityId, RequestDate = requestDate },
                            tr);
                        await tr.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await tr.RollbackAsync();
                        Logger.LogError(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Add requests for users. User cannot be new, cities cannot be new.
        /// Application sends requests for existing users only according to their last city requests, so we assume there is no new user/city.
        /// It's enough for testing purposes, but should be modified in real application.
        /// </summary>
        /// <param name="requests">Requests</param>
        /// <param name="requestDate">Requests date</param>
        public async Task AddRequestsAsync(IEnumerable<WeatherRequest> requests, DateTime requestDate)
        {
            using (var connection = new SqlConnection(GetDBConnectionString()))
            {
                await connection.OpenAsync();
                using (var tr = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var request in requests)
                        {
                            await connection.ExecuteAsync(ADD_REQUEST,
                                new { request.UserId, request.CityId, RequestDate = requestDate },
                                tr);
                        }
                        await tr.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await tr.RollbackAsync();
                        Logger.LogError(ex.Message);
                    }
                }
            }
        }

        private string GetDBConnectionString()
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();

            sqlBuilder.DataSource = DATABASE_SERVERNAME;

            // In real application this value will be defined in config
            sqlBuilder.AttachDBFilename = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\Data", DATABASE_FILENAME));

            sqlBuilder.IntegratedSecurity = true;
            sqlBuilder.ConnectTimeout = 30;
            sqlBuilder.MultipleActiveResultSets = true;

            return sqlBuilder.ToString();
        }
    }
}
