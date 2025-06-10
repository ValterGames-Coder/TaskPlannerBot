using Scheduling;
using System.Collections.Concurrent;

internal static class ExtensionSchedule
{
    public static Schedule Day(this Schedule schedule, int day)
    {
        return day switch
        {
            0 => schedule.Monday(),
            1 => schedule.Tuesday(),
            2 => schedule.Wednesday(),
            3 => schedule.Thursday(),
            4 => schedule.Friday(),
            5 => schedule.Saturday(),
            6 => schedule.Sunday(),
            _ => throw new ArgumentOutOfRangeException(nameof(day), day, null)
        };
    }
}

namespace SchedulerBot
{
    public class Scheduler
    {
        public event Action<string>? OnTaskCall;

        private readonly ConcurrentQueue<ScheduleTask> _pendingTasks = new();
        private readonly Timer _taskProcessingTimer;

        public Scheduler()
        {
            _taskProcessingTimer = new Timer(_ => ProcessPendingTasks(), null, 0, 1000); // 1 sec.
        }

        public async Task Initialize()
        {
            List<ScheduleTask> tasks = await Database.GetSchedule();
            foreach (ScheduleTask task in tasks)
                _pendingTasks.Enqueue(task);

            Database.OnTaskAdded += task => _pendingTasks.Enqueue(task);
        }

        private void ProcessPendingTasks()
        {
            while (_pendingTasks.TryDequeue(out var task))
                AddTaskToSchedule(task);
        }

        private void AddTaskToSchedule(ScheduleTask task)
        {
            int[] days = task.Dayweek!.Split(",").Select(int.Parse).ToArray();

            foreach (int day in days)
            {
                Schedule.Every().Day(day).At($"{task.StartTime.AddMinutes(-5):HH\\:mm}").Run(
                    () => TaskCall($"<b>🔔 Через 5 минут начнется задача: \"{task.Id}.{task.Name}\"!</b>\n"));
                Schedule.Every().Day(day).At($"{task.StartTime:HH\\:mm}").Run(
                    () => TaskCall($"<b>🔔 Задача \"{task.Id}.{task.Name}\" началась!</b>\n"));
                Schedule.Every().Day(day).At($"{task.EndTime:HH\\:mm}").Run(
                    () => TaskCall($"<b>🔔 Задача \"{task.Id}.{task.Name}\" окончилась!</b>\n"));
            }
        }

        private void TaskCall(string text) => OnTaskCall?.Invoke(text);
    }
}