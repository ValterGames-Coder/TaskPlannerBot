using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SchedulerBot;

public class DeleteTaskCommandHandler : StateHandler
{
    public override async Task<bool> MakeStep(TelegramBotClient client, Message message)
    {
        string? text = message.Text;
        long chatId = message.Chat.Id;

        switch (Step)
        {
            case 0:
            {
                var newMessage = await client.SendMessage(chatId, "Введите ID задачи (или задач через запятую):");
                MessageId = newMessage.Id;
                Step++;
                break;
            }
            case 1:
            {
                int[] taskIdArray = text!.Split(",").Select(int.Parse).ToArray();
                await Database.DeleteTask(taskIdArray);
                Step++;
                await client.EditMessageText(chatId, MessageId, "✅ Задачи успешно удалены!");
                return true;
            }
        }
        await client.DeleteMessage(chatId, message.Id);
        return false;
    }
}