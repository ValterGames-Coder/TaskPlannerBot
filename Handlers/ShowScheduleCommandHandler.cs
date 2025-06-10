using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchedulerBot;

public class ShowScheduleCommandHandler : StateHandler
{
    public override async Task<bool> MakeStep(TelegramBotClient client, Message message)
    {
        long chatId = message.Chat.Id;
        List<ScheduleTask> tasks = await Database.GetSchedule(Step);
        
        string[] days = ["Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];

        var replyMarkup = new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData("⬅️", "l"),
                InlineKeyboardButton.WithCallbackData("➡️", "r")
            ]
        ]);

        string text = $"<b>~ {days[Step]} ~</b>\n\n";

        if (tasks.Count == 0)
            text += "На этот день нет задач! 🙌";
        else
            foreach (var task in tasks)
                text += $"<b>📌 {task.Name}</b>\n" +
                        $"🆔 {task.Id}. ⏰ {task.StartTime:HH\\:mm} - {task.EndTime:HH\\:mm}\n\n";
            
        if (MessageId == 0)
        {
            var msg = await client.SendMessage(chatId, text, replyMarkup: replyMarkup, parseMode: ParseMode.Html);
            MessageId = msg.Id;
        }
        else
            await client.EditMessageText(chatId, MessageId, text, replyMarkup: replyMarkup, parseMode: ParseMode.Html);

        return false;
    }
}