using AutoMapper;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models;

namespace CRM.Features.CRM.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Contact, ContactResponse>()
            .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company != null ? s.Company.Name : null))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Source, o => o.MapFrom(s => s.Source.ToString()))
            .ForMember(d => d.Tags, o => o.MapFrom(s => s.ContactTags.Select(ct => ct.Tag)))
            .ForMember(d => d.Deals, o => o.MapFrom(s => s.Deals))
            .ForMember(d => d.RecentActivities, o => o.MapFrom(s => s.Activities.OrderByDescending(a => a.CreatedAt).Take(5)));

        CreateMap<Company, CompanyResponse>()
            .ForMember(d => d.ContactsCount, o => o.MapFrom(s => s.Contacts.Count))
            .ForMember(d => d.OpenDealsCount, o => o.MapFrom(s => s.Deals.Count(d => d.ClosedAt == null)))
            .ForMember(d => d.OpenDealsValue, o => o.MapFrom(s => s.Deals.Where(d => d.ClosedAt == null).Sum(d => d.Value)));

        CreateMap<Deal, DealResponse>()
            .ForMember(d => d.Stage, o => o.MapFrom(s => s.Stage.ToString()))
            .ForMember(d => d.Contact, o => o.MapFrom(s => s.Contact))
            .ForMember(d => d.Company, o => o.MapFrom(s => s.Company))
            .ForMember(d => d.Activities, o => o.MapFrom(s => s.Activities.OrderByDescending(a => a.CreatedAt)))
            .ForMember(d => d.Notes, o => o.MapFrom(s => s.Notes.OrderByDescending(n => n.CreatedAt)));

        CreateMap<Activity, ActivityResponse>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));
            
        CreateMap<Activity, ActivitySummaryDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));

        CreateMap<Note, NoteResponse>();

        CreateMap<Tag, TagDto>();

        CreateMap<Contact, ContactSummaryDto>();
        CreateMap<Company, CompanySummaryDto>();

        CreateMap<Deal, DealSummaryDto>()
            .ForMember(d => d.Stage, o => o.MapFrom(s => s.Stage.ToString()));
    }
}
