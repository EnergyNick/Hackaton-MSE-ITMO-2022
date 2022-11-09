﻿using AutoMapper;
using StudentManager.Service.Models.Subjects;
using StudentManager.Service.Models.Users;
using StudentManager.Tables.Models;

namespace StudentManager.Service.Models.MappingProfiles;

public class DomainMappingProfile : Profile
{
    public DomainMappingProfile()
    {
        CreateMap<StudentData, StudentDto>()
            .ForMember(x => x.FirstName, dest => dest.MapFrom(x => x.Name))
            .ForMember(x => x.LastName, dest => dest.MapFrom(x => x.Surname))
            .ForMember(x => x.GroupId, dest => dest.MapFrom(x => x.IdGroup))
            .ForMember(x => x.TelegramId, dest => dest.MapFrom(x => x.Telegram));

        CreateMap<AcademicSubjectData, SubjectDto>()
            .ForMember(x => x.Name, dest => dest.MapFrom(x => x.Title))
            .ForMember(x => x.Semestr, dest => dest.MapFrom(x => x.Term))
            .ForMember(x => x.GroupId, dest => dest.MapFrom(x => x.IdGroup))
            .ForMember(x => x.CscLink, dest => dest.MapFrom(x => x.LinkToCSC))
            .ForMember(x => x.LectorId, dest => dest.MapFrom(x => x.IdTeacher));

        CreateMap<AcademicSubjectData, SubjectInfoDto>()
            .ForMember(x => x.Name, dest => dest.MapFrom(x => x.Title));
    }
}