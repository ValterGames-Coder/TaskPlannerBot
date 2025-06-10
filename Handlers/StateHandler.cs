using Telegram.Bot.Types;
using Telegram.Bot;

namespace SchedulerBot
{
    public abstract class StateHandler
    {
        public int Step;
        public int MessageId;

        public abstract Task<bool> MakeStep(TelegramBotClient client, Message message);
    }
}
