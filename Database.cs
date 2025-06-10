using Microsoft.Data.Sqlite;
using System.Data;
using Telegram.Bot.Types;

namespace SchedulerBot;

public enum SqliteCommandType
{
    NonQuery,
    Reader,
    Scalar
}

public static class Database
{
    private const string ConnectionParameters = "Data Source=database.db;Cache=Shared;";
    public static event Action<ScheduleTask>? OnTaskAdded;

    private static async Task<object?> Execute(
        string commandText,
        (string, object?)[]? parameters = null,
        SqliteCommandType commandType = SqliteCommandType.NonQuery)
    {
        var connection = new SqliteConnection(ConnectionParameters);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = commandText;

        if (parameters != null)
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);

        return commandType switch
        {
            SqliteCommandType.NonQuery => await command.ExecuteNonQueryAsync()
                .ContinueWith(t =>
                {
                    connection.Dispose();
                    command.Dispose();
                    return (object?)t.Result;
                }),

            SqliteCommandType.Scalar => await command.ExecuteScalarAsync()
                .ContinueWith(t =>
                {
                    connection.Dispose();
                    command.Dispose();
                    return t.Result;
                }),

            SqliteCommandType.Reader => await command.ExecuteReaderAsync(CommandBehavior.CloseConnection),
            _ => throw new ArgumentOutOfRangeException(nameof(commandType))
        };
    }


    public static async Task<bool> Init()
    {
        try
        {
            await Execute("""
                              CREATE TABLE IF NOT EXISTS "Tasks" (
                          	"id"	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                          	"name"	TEXT NOT NULL,
                          	"dayweek"	TEXT NOT NULL,
                          	"startTime"	DATETIME NOT NULL,
                          	"endTime"	DATETIME NOT NULL);
                          """);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database catch error: {ex.Message}");
            return false;
        }
    }

    public static async Task AddTask(ScheduleTask task)
    {
        try
        {
            await Execute(
                """
                INSERT INTO Tasks (name, dayweek, startTime, endTime)
                VALUES (@name, @day, @start, @end)
                """,
                [
                    ("@name",  task.Name),
                    ("@day",   task.Dayweek),
                    ("@start", task.StartTime),
                    ("@end",   task.EndTime)
                ]
            );
            OnTaskAdded?.Invoke(task);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding task: {ex.Message}");
            throw;
        }
    }
    
    public static async Task DeleteTask(int[] taskIdArray)
    {
        try
        {
            foreach (int id in taskIdArray)
            {
                await Execute(
                    """
                    DELETE FROM Tasks WHERE id = @id
                    """,
                    [("@id",  id)]
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error delete task: {ex.Message}");
            throw;
        }
    }
    
    public static async Task ResetDatabase()
    {
        try
        {
            await Execute(
                    """
                    DELETE FROM Tasks
                    """);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error delete task: {ex.Message}");
            throw;
        }
    }

    public static async Task<List<ScheduleTask>> GetSchedule(int day = -1)
    {
        try
        {
            var result = await Execute(
                """
                SELECT * FROM Tasks
                """,
                commandType: SqliteCommandType.Reader);
            await using var reader = result as SqliteDataReader ?? throw new Exception("SqliteDataReader Error");

            List<ScheduleTask> scheduleTasks = [];
            while (await reader.ReadAsync())
            {
                if (day != -1)
                    if (!reader.GetString(2).Contains(day.ToString()))
                        continue;

                scheduleTasks.Add(new ScheduleTask()
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Dayweek = reader.GetString(2),
                    StartTime = reader.GetDateTime(3),
                    EndTime = reader.GetDateTime(4)
                });
            }
            return scheduleTasks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get schedule error: {ex.Message}");
            throw;
        }
    }
}