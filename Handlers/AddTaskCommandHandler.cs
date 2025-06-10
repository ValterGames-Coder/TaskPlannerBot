using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchedulerBot
{
    public class AddTaskCommandHandler : StateHandler
    {
        public ScheduleTask Task = new();

        public override async Task<bool> MakeStep(TelegramBotClient client, Message message)
        {
            string? text = message.Text;
            long chatId = message.Chat.Id;

            switch (Step)
            {
                case 0:
                {
                    var newMessage = await client.SendMessage(chatId, "Введите название задачи:");
                    MessageId = newMessage.Id;
                    Step++;
                    break;
                }
                case 1:
                {
                    Task.Name = text;
                    Step++;
                    await client.EditMessageText(chatId, MessageId, "Выберите дни недели (после выбора напиши \"Готово\"):",
                        replyMarkup: GetDayButtons(new HashSet<int>()));
                    break;
                }
                case 2:
                {
                    if (text != "Готово")
                        return false;
                    Step++;
                    await client.EditMessageText(chatId, MessageId, "Напиши время начала задачи (HH:MM):");
                    break;
                }
                case 3:
                {
                    if (DateTime.TryParse(text, out var startTime))
                    {
                        Task.StartTime = startTime;
                        Step++;
                        await client.EditMessageText(chatId, MessageId, "Напиши время окончания задачи (HH:MM):");
                    }
                    break;
                }
                case 4:
                {
                    if (DateTime.TryParse(text, out var endTime))
                    {
                        Task.EndTime = endTime;
                        await Database.AddTask(Task);
                        await client.EditMessageText(chatId, MessageId, $"✅ {Task.Name} успешно добавлена!");
                        await client.DeleteMessage(chatId, message.Id);
                        Step = 0;
                        return true;
                    }
                    break;
                }
            }
            await client.DeleteMessage(chatId, message.Id);
            return false;
        }
        
        public static InlineKeyboardMarkup GetDayButtons(HashSet<int> selectedDays)
        {
            InlineKeyboardButton[][] rows =
            [
                [CreateDayButton("Пн", 0, selectedDays), CreateDayButton("Вт", 1, selectedDays), CreateDayButton("Ср", 2, selectedDays) ],
                [CreateDayButton("Чт", 3, selectedDays), CreateDayButton("Пт", 4, selectedDays), CreateDayButton("Сб", 5, selectedDays) ],
                [CreateDayButton("Вс", 6, selectedDays)]
            ];
            return new InlineKeyboardMarkup(rows);
        }

        private static InlineKeyboardButton CreateDayButton(string label, int value, HashSet<int> selected)
        {
            string text = selected.Contains(value) ? $"✅ {label}" : label;
            return InlineKeyboardButton.WithCallbackData(text, $"day:{value}");
        }
    }
}
