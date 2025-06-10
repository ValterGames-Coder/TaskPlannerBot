using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchedulerBot
{
    public static class CallbackQueryHandler
    {
        public static async Task ScheduleTask(TelegramBotClient client, CallbackQuery callbackQuery, AddTaskCommandHandler handler)
        {
            if (callbackQuery.Data?.StartsWith("day:") == true)
            {
                int day = int.Parse(callbackQuery.Data.Split(':')[1]);

                HashSet<int> hashDays = [];
                if (handler.Task.Dayweek != null)
                    hashDays = handler.Task.Dayweek.Split(',').Select(int.Parse).ToHashSet();

                if (!hashDays.Remove(day))
                    hashDays.Add(day);

                handler.Task.Dayweek = string.Join(",", hashDays);

                var newMarkup = AddTaskCommandHandler.GetDayButtons(hashDays);
                await client.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, handler.MessageId, newMarkup);
                await client.AnswerCallbackQuery(callbackQuery.Id);
            }
        }

        public static async Task ShowSchedule(TelegramBotClient client, CallbackQuery callbackQuery, ShowScheduleCommandHandler handler)
        {
            handler.Step = callbackQuery.Data switch
            {
                "l" => (handler.Step + 6) % 7,
                "r" => (handler.Step + 1) % 7,
                _ => handler.Step
            };
            await handler.MakeStep(client, callbackQuery.Message!);
            await client.AnswerCallbackQuery(callbackQuery.Id);
        }
    }
}
