using ERP.API.Contracts.Admin;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicContentController(ErpDbContext dbContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("landing-content")]
    [ProducesResponseType(typeof(IReadOnlyList<LandingPageContentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LandingPageContentDto>>> GetLandingContent(CancellationToken cancellationToken)
    {
        var items = await dbContext.LandingPageContents
            .AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Key)
            .Select(x => new LandingPageContentDto(
                x.Key,
                x.Title,
                x.Content,
                x.IsPublished,
                x.SortOrder,
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}
