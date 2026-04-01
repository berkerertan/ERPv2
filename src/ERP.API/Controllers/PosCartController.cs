using ERP.API.Common;
using ERP.API.Contracts.PosCart;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PosCartController(ErpDbContext dbContext) : ControllerBase
{
    private const int MaxOpenCartCount = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedPaymentMethods = ["cash", "card", "credit"];

    [HttpGet("List")]
    [RequirePolicy("TierUserOrAdmin")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Pos)]
    [ProducesResponseType(typeof(IReadOnlyList<PosCartSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PosCartSummaryResponse>>> List(CancellationToken cancellationToken)
    {
        var carts = await dbContext.PosCarts
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Take(MaxOpenCartCount)
            .ToListAsync(cancellationToken);

        var response = carts
            .Select(cart =>
            {
                var items = DeserializeItems(cart.ItemsJson);
                var updatedAt = cart.UpdatedAtUtc ?? cart.CreatedAtUtc;
                return new PosCartSummaryResponse(
                    cart.Id,
                    cart.ShareToken,
                    cart.Label,
                    cart.BuyerName,
                    cart.PaymentMethod,
                    items.Count,
                    items.Sum(x => x.Total),
                    cart.CreatedAtUtc,
                    updatedAt);
            })
            .ToList();

        return Ok(response);
    }

    [HttpPost("Save")]
    [RequirePolicy("TierUserOrAdmin")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Pos)]
    [ProducesResponseType(typeof(SavePosCartResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SavePosCartResponse>> Save(
        [FromBody] SavePosCartRequest request,
        CancellationToken cancellationToken)
    {
        if (request.WarehouseId == Guid.Empty)
        {
            return BadRequest("WarehouseId is required.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest("At least one cart item is required.");
        }

        var normalizedItems = NormalizeItems(request.Items);
        if (normalizedItems.Count == 0)
        {
            return BadRequest("Cart items are invalid.");
        }

        if (!await dbContext.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, cancellationToken))
        {
            return BadRequest("Warehouse not found.");
        }

        if (request.BuyerId.HasValue)
        {
            var buyerExists = await dbContext.CariAccounts.AnyAsync(x => x.Id == request.BuyerId.Value, cancellationToken);
            if (!buyerExists)
            {
                return BadRequest("Buyer not found.");
            }
        }

        var normalizedPaymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        if (normalizedPaymentMethod is null)
        {
            return BadRequest("PaymentMethod must be one of: cash, card, credit.");
        }

        var now = DateTime.UtcNow;
        var label = NormalizeLabel(request.Label, now);
        if (label is null)
        {
            return BadRequest("Label cannot exceed 120 characters.");
        }

        PosCart cart;
        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            var existingCart = await dbContext.PosCarts.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);
            if (existingCart is null)
            {
                return NotFound("Cart not found.");
            }

            cart = existingCart;
        }
        else
        {
            var existingCarts = await dbContext.PosCarts
                .OrderBy(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (existingCarts.Count >= MaxOpenCartCount)
            {
                existingCarts[0].MarkAsDeleted(now);
            }

            cart = new PosCart
            {
                Label = label,
                ShareToken = await GenerateShareTokenAsync(cancellationToken),
                BuyerId = request.BuyerId,
                BuyerName = NormalizeBuyerName(request.BuyerName),
                PaymentMethod = normalizedPaymentMethod,
                WarehouseId = request.WarehouseId,
                ItemsJson = SerializeItems(normalizedItems),
                UpdatedAtUtc = now
            };

            dbContext.PosCarts.Add(cart);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new SavePosCartResponse(cart.Id, cart.ShareToken, cart.Label, cart.UpdatedAtUtc ?? cart.CreatedAtUtc));
        }

        cart.Label = label;
        cart.BuyerId = request.BuyerId;
        cart.BuyerName = NormalizeBuyerName(request.BuyerName);
        cart.PaymentMethod = normalizedPaymentMethod;
        cart.WarehouseId = request.WarehouseId;
        cart.ItemsJson = SerializeItems(normalizedItems);
        cart.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new SavePosCartResponse(cart.Id, cart.ShareToken, cart.Label, cart.UpdatedAtUtc ?? cart.CreatedAtUtc));
    }

    [HttpDelete("Delete/{id:guid}")]
    [RequirePolicy("TierUserOrAdmin")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Pos)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var cart = await dbContext.PosCarts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cart is null)
        {
            return NotFound("Cart not found.");
        }

        cart.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok("Deleted");
    }

    [AllowAnonymous]
    [HttpGet("ByToken/{token}")]
    [ProducesResponseType(typeof(PosCartDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PosCartDetailResponse>> ByToken(string token, CancellationToken cancellationToken)
    {
        var normalizedToken = (token ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return BadRequest("Token is required.");
        }

        var cart = await dbContext.PosCarts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.ShareToken == normalizedToken, cancellationToken);

        if (cart is null)
        {
            return NotFound("Cart not found.");
        }

        var items = DeserializeItems(cart.ItemsJson);
        var updatedAt = cart.UpdatedAtUtc ?? cart.CreatedAtUtc;

        return Ok(new PosCartDetailResponse(
            cart.Id,
            cart.ShareToken,
            cart.Label,
            cart.BuyerId,
            cart.BuyerName,
            cart.PaymentMethod,
            cart.WarehouseId,
            items.Select(x => new PosCartItemContract(
                x.ProductId,
                x.Name,
                x.Barcode,
                x.Quantity,
                x.UnitPrice,
                x.Total)).ToList(),
            cart.CreatedAtUtc,
            updatedAt));
    }

    private static string? NormalizePaymentMethod(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "cash";
        }

        return AllowedPaymentMethods.Contains(normalized) ? normalized : null;
    }

    private static string? NormalizeLabel(string? value, DateTime nowUtc)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return $"Sepet {nowUtc:HH:mm}";
        }

        return normalized.Length > 120 ? null : normalized;
    }

    private static string? NormalizeBuyerName(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length > 150 ? normalized[..150] : normalized;
    }

    private static List<PersistedPosCartItem> NormalizeItems(IReadOnlyList<PosCartItemContract> items)
    {
        var normalized = new List<PersistedPosCartItem>(items.Count);
        foreach (var item in items)
        {
            if (item is null)
            {
                continue;
            }

            if (item.Quantity <= 0 || item.UnitPrice < 0)
            {
                continue;
            }

            var name = (item.Name ?? string.Empty).Trim();
            var barcode = (item.Barcode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var safeName = name.Length > 200 ? name[..200] : name;
            var safeBarcode = barcode.Length > 60 ? barcode[..60] : barcode;
            normalized.Add(new PersistedPosCartItem(
                item.ProductId,
                safeName,
                safeBarcode,
                item.Quantity,
                item.UnitPrice,
                item.Quantity * item.UnitPrice));
        }

        return normalized;
    }

    private static List<PosCartItemContract> DeserializeItems(string? itemsJson)
    {
        if (string.IsNullOrWhiteSpace(itemsJson))
        {
            return [];
        }

        try
        {
            var persisted = JsonSerializer.Deserialize<List<PersistedPosCartItem>>(itemsJson, JsonOptions) ?? [];
            return persisted.Select(x => new PosCartItemContract(
                x.ProductId,
                x.Name,
                x.Barcode,
                x.Quantity,
                x.UnitPrice,
                x.Total)).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeItems(IReadOnlyList<PersistedPosCartItem> items)
    {
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    private async Task<string> GenerateShareTokenAsync(CancellationToken cancellationToken)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[10];

        for (var attempt = 0; attempt < 20; attempt++)
        {
            RandomNumberGenerator.Fill(bytes);
            var tokenChars = new char[10];
            for (var i = 0; i < tokenChars.Length; i++)
            {
                tokenChars[i] = alphabet[bytes[i] % alphabet.Length];
            }

            var token = new string(tokenChars);
            var exists = await dbContext.PosCarts
                .IgnoreQueryFilters()
                .AnyAsync(x => x.ShareToken == token, cancellationToken);

            if (!exists)
            {
                return token;
            }
        }

        return Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    }

    private sealed record PersistedPosCartItem(
        Guid? ProductId,
        string Name,
        string Barcode,
        decimal Quantity,
        decimal UnitPrice,
        decimal Total);
}
