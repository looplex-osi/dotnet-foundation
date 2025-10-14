using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Queries;
using Looplex.Samples.Application.Commands;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Newtonsoft.Json.Linq;

namespace Looplex.Samples.Application.Services;

/// <summary>
/// SCIMv2 Resource Service for Notes
/// Inherits from BaseResourceService<Note> to leverage Foundation's generic implementation
/// </summary>
public class NoteResourceService : BaseResourceService<Note>
{
    private readonly IMediator _mediator;

    public NoteResourceService(IMediator mediator, IResourceRepository<Note> repository) 
        : base(repository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public override string CollectionName => "Notes";

    // All CRUD operations are now inherited from BaseResourceService<Note>
    // The base class provides complete SCIM v2.0 compliant implementations
}
