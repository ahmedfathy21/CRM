using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models.Enums;
using MediatR;

namespace CRM.Features.CRM.Contacts.Commands.CreateContact;

public record CreateContactCommand(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    ContactStatus Status,
    ContactSource Source,
    Guid? CompanyId,
    List<Guid> TagIds
) : IRequest<Result<ContactResponse>>;
