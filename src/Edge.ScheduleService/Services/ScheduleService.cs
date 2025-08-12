using Edge.Data;
using Edge.Data.Entities;
using Edge.ScheduleService.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Edge.ScheduleService.Services;

public class ScheduleService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<ScheduleService> _logger;
    private IScheduler? _scheduler;

    public ScheduleService(
        IServiceScopeFactory scopeFactory,
        ISchedulerFactory schedulerFactory,
        ILogger<ScheduleService> logger)
    {
        _scopeFactory = scopeFactory;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _scheduler = await _schedulerFactory.GetScheduler(stoppingToken);
        await _scheduler.Start(stoppingToken);

        _logger.LogInformation("Schedule service started. Loading schedules...");

        await LoadAndScheduleJobs(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            await LoadAndScheduleJobs(stoppingToken);
        }
    }

    private async Task LoadAndScheduleJobs(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EdgeDbContext>();

            var now = DateTime.UtcNow;
            var futureLimit = now.AddHours(1);

            var activeSchedules = await context.Schedules
                .Where(s => s.IsActive && 
                           s.ScheduledTimeUtc > now && 
                           s.ScheduledTimeUtc <= futureLimit)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} active schedules to process", activeSchedules.Count);

            foreach (var schedule in activeSchedules)
            {
                var jobKey = new JobKey($"schedule-{schedule.Id}", "schedules");
                
                if (await _scheduler!.CheckExists(jobKey, cancellationToken))
                {
                    continue;
                }

                var job = JobBuilder.Create<ScheduleJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("ScheduleId", schedule.Id)
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"trigger-{schedule.Id}", "schedules")
                    .StartAt(DateTimeOffset.FromUnixTimeSeconds(
                        ((DateTimeOffset)schedule.ScheduledTimeUtc).ToUnixTimeSeconds()))
                    .Build();

                await _scheduler.ScheduleJob(job, trigger, cancellationToken);

                _logger.LogInformation("Scheduled job for Schedule {ScheduleId} at {ScheduledTime}", 
                    schedule.Id, schedule.ScheduledTimeUtc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading and scheduling jobs");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}