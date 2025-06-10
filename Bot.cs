using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchedulerBot;

public static class ButtonTexts
{
    public const string AddTask = "📝 Добавить задачу";
    public const string ShowSchedule = "📋 Посмотреть расписание";
    public const string DeleteTasks = "🗑 Удалить задачи";
}

public static class Bot
{
    private static TelegramBotClient? _client;
    private static readonly Dictionary<string, Func<StateHandler>>? CommandHandlers = new()
    {
        {ButtonTexts.AddTask, () => new AddTaskCommandHandler()},
        {ButtonTexts.ShowSchedule, () => new ShowScheduleCommandHandler()},
        {ButtonTexts.DeleteTasks, () => new DeleteTaskCommandHandler()}
    };
    private static readonly ReplyKeyboardMarkup MainMenuKeyboard = new([
        [ButtonTexts.AddTask, ButtonTexts.DeleteTasks],
        [ButtonTexts.ShowSchedule]
    ]) { ResizeKeyboard = true };
    
    private static StateHandler? _currentState;
    private static long _chatId;

    private static async Task Main(string[] args)
    {
        EnvReader.Load(".env");
        string? token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
                        ?? throw new Exception("Token is null!");

        if (await Database.Init() == false)
            return;

        _client = new TelegramBotClient(token);
        _client.OnMessage += OnMessage;
        _client.OnUpdate += OnUpdate;

        var scheduler = new Scheduler();
        await scheduler.Initialize();
        scheduler.OnTaskCall += async (text) => await TaskCall(text);
        
        Console.WriteLine("Bot is worked!");
        Console.ReadLine();
    }

    private static async Task WithClient(Func<TelegramBotClient, Task> action)
    {
        if (_client is null)
            return;

        await action(_client);
    }

    private static async Task OnUpdate(Update update)
    {
        switch (update)
        {
            case { CallbackQuery: { } callbackQuery }:
                await OnCallbackQuery(callbackQuery);
                break;
            default:
                Console.WriteLine($"Received unhandled update {update.Type}");
                break;
        };
    }

    private static async Task OnMessage(Message message, UpdateType type)
    {
        if (message.Text is null)
            return;
        
        _chatId = message.Chat.Id;

        await WithClient(async client =>
        {
            if (CommandHandlers!.TryGetValue(message.Text, out var handler))
            {
                _currentState = handler();
                await _currentState.MakeStep(client, message);
            }
            else if (message.Text.StartsWith('/'))
            {
                _currentState = null;
                string[] commandParts = message.Text.Split(" ");
                string command = commandParts[0];
                string args = commandParts.Length > 1 ? commandParts[1] : String.Empty;
                await OnCommand(command, args, message);
            }
            else if (_currentState != null)
            {
                bool isCompleted = await _currentState.MakeStep(client, message);
                if (isCompleted)
                    _currentState = null;
            }
            else
                await client.SendMessage(_chatId, "⚠️ Неизвестная команда!");
        });
    }

    private static async Task OnCommand(string command, string args, Message message)
    {
        await WithClient(async client =>
        {
            switch (command)
            {
                case "/start":
                    await client.SendMessage(_chatId, "Бот активен!",
                        replyMarkup: MainMenuKeyboard);
                    break;
                case "/reset":
                    await Database.ResetDatabase();
                    await client.SendMessage(_chatId, "✅ База данных была очищена",
                        replyMarkup: MainMenuKeyboard);
                    break;
                default:
                    await client.SendMessage(_chatId, "⚠️ Неизвестная команда!");
                    break;
            }
        });
    }

    private static async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        await WithClient(async client =>
        {
            switch (_currentState)
            {
                case AddTaskCommandHandler addTaskCommandHandler:
                    await CallbackQueryHandler.ScheduleTask(client, callbackQuery, addTaskCommandHandler);
                    break;
                case ShowScheduleCommandHandler showScheduleCommandHandler:
                    await CallbackQueryHandler.ShowSchedule(client, callbackQuery, showScheduleCommandHandler);
                    break;
            }
        });
    }

    private static async Task TaskCall(string text)
    {
        if (_chatId == 0)
            throw new NullReferenceException("ChatID is not init!");
        
        await WithClient(async client =>
        {
            await client.SendMessage(_chatId, text, ParseMode.Html);
        });
    }
}