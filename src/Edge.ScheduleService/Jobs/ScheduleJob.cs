using Edge.Data;
using Edge.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Edge.ScheduleService.Jobs;

public class ScheduleJob : IJob
{
    private readonly EdgeDbContext _context;
    private readonly ILogger<ScheduleJob> _logger;

    public ScheduleJob(EdgeDbContext context, ILogger<ScheduleJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var scheduleId = context.JobDetail.JobDataMap.GetIntValue("ScheduleId");
        
        var schedule = await _context.Schedules
            .Include(s => s.Area)
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.IsActive);

        if (schedule == null)
        {
            _logger.LogWarning("Schedule {ScheduleId} not found or inactive", scheduleId);
            return;
        }

        _logger.LogInformation("Executing schedule {ScheduleId}: {EventType} - {Name} for Area {AreaName} (ID: {AreaId})", 
            schedule.Id, 
            schedule.EventType, 
            schedule.Name, 
            schedule.Area.Name, 
            schedule.AreaId);

        _logger.LogInformation("Schedule Event Data: {EventData}", schedule.EventData);

        schedule.LastExecutedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule {ScheduleId} executed successfully at {ExecutedAt}", 
            schedule.Id, 
            schedule.LastExecutedAt);
    }
}