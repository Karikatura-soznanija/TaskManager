using AutoMapper;
using TaskManager.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TaskManager.Profiles;

public class TaskProfile : Profile
{
    public TaskProfile()
    {
        // TaskCreateDto → TaskItem
        CreateMap<TaskCreateDto, TaskItem>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow));

        // TaskItem → TaskReadDto
        CreateMap<TaskItem, TaskReadDto>();
    }
}
