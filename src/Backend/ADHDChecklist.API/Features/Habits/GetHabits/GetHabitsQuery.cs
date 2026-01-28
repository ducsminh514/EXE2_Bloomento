using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Features.Habits.GetHabits
{
    public record GetHabitsQuery(
        Guid UserId
    ) : IRequest<List<HabitResponse>>;
}
