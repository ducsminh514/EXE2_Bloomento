using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;

namespace ADHDChecklist.API.Features.Tasks.GetTasksByDate
{
    public class GetTasksByDateQueryHandler : IRequestHandler<GetTasksByDateQuery, TaskListResponse>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GetTasksByDateQueryHandler> _logger;

        public GetTasksByDateQueryHandler(
            AppDbContext context,
            ILogger<GetTasksByDateQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TaskListResponse> Handle(GetTasksByDateQuery request, CancellationToken cancellationToken)
        {
            var tasks = await _context.Tasks
                .Where(t => t.UserId == request.UserId && t.ScheduledDate == request.Date)
                .Include(t => t.Category)
                //.Include(t => t.SubTasks)
                .OrderBy(t => t.TimeBlockStart ?? TimeOnly.MaxValue) // Scheduled first, then inbox
                .ThenBy(t => t.OrderIndex)
                .Select(t => new TaskResponse(
                    t.Id,
                    t.Title,
                    t.Description,
                    t.CategoryId,
                    t.Category != null ? t.Category.Name : null,
                    t.Category != null ? t.Category.ColorHex : null,
                    t.ScheduledDate,
                    t.TimeBlockStart,
                    t.TimeBlockEnd,
                    t.Duration,
                    t.IsCompleted,
                    t.CompletedAt,
                    (int)t.Priority,
                    t.IsRecurring,
                    t.RecurrencePattern,
                    t.ParentTaskId,
                    new List<TaskResponse>(),
                    //t.SubTasks.Select(st => new TaskResponse(
                    //    st.Id,
                    //    st.Title,
                    //    st.Description,
                    //    st.CategoryId,
                    //    null,
                    //    null,
                    //    st.ScheduledDate,
                    //    st.TimeBlockStart,
                    //    st.TimeBlockEnd,
                    //    st.Duration,
                    //    st.IsCompleted,
                    //    st.CompletedAt,
                    //    (int)st.Priority,
                    //    st.IsRecurring,
                    //    st.RecurrencePattern,
                    //    st.ParentTaskId,
                    //    new List<TaskResponse>(),
                    //    st.OrderIndex,
                    //    st.CreatedAt,
                    //    st.UpdatedAt
                    //)).ToList(),
                    t.OrderIndex,
                    t.CreatedAt,
                    t.UpdatedAt
                ))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} tasks for user {UserId} on {Date}",
                tasks.Count, request.UserId, request.Date);

            return new TaskListResponse(
                tasks,
                tasks.Count,
                request.Date
            );
        }
    }
}
