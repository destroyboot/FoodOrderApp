using API.Support;
using Core.Contracts.Orders;
using Core.Data.Entities;
using Core.Data.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace API.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly IShoppingCartService _cart;
        private readonly AppDbContext _db;
        private readonly IEmailSender _email;
        private readonly IOrderSummaryEmailComposer _orderEmails;

        public CartController(IShoppingCartService cart, AppDbContext db, IEmailSender email, IOrderSummaryEmailComposer orderEmails)
        {
            _cart = cart;
            _db = db;
            _email = email;
            _orderEmails = orderEmails;
        }

        private string? CustomerId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        private string? GuestToken
            => Request.Headers.TryGetValue("X-Guest-Token", out var v) ? v.ToString() : null;

        private void EnsureOwnerProvided()
        {
            if (string.IsNullOrWhiteSpace(CustomerId) && string.IsNullOrWhiteSpace(GuestToken))
                throw new InvalidOperationException("Provide X-Guest-Token header or authenticate with JWT.");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<CartCreateResponseDto>> Create([FromBody] CartCreateRequestDto? dto, CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.CreateCartAsync(CustomerId, GuestToken, dto?.RestaurantId, ct);
            return Ok(result);
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<IReadOnlyList<ActiveCartSummaryDto>>> GetActive(CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.GetActiveCartsAsync(CustomerId, GuestToken, ct);
            return Ok(result);
        }

        [HttpGet("{cartId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<CartResponseDto>> Get(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.GetCartAsync(cartId, CustomerId, GuestToken, ct);
            return Ok(result);
        }

        [HttpPut("{cartId:int}/meta")]
        [AllowAnonymous]
        public async Task<IActionResult> SetMeta(int cartId, [FromBody] CartSetMetaDto dto, CancellationToken ct)
        {
            EnsureOwnerProvided();
            await _cart.SetMetaAsync(cartId, dto, CustomerId, GuestToken, ct);
            return NoContent();
        }

        [HttpPut("{cartId:int}/items")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateItems(int cartId, [FromBody] CartUpdateItemsDto dto, CancellationToken ct)
        {
            EnsureOwnerProvided();
            await _cart.UpdateItemsAsync(cartId, dto, CustomerId, GuestToken, ct);
            return NoContent();
        }

        [HttpDelete("{cartId:int}/items")]
        [AllowAnonymous]
        public async Task<IActionResult> ClearItems(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            await _cart.ClearAsync(cartId, CustomerId, GuestToken, ct);
            return NoContent();
        }

        [HttpDelete("{cartId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Delete(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            await _cart.DeleteAsync(cartId, CustomerId, GuestToken, ct);
            return NoContent();
        }

        [HttpPost("{cartId:int}/preview")]
        [AllowAnonymous]
        public async Task<ActionResult<CartPreviewResponseDto>> Preview(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.PreviewAsync(cartId, CustomerId, GuestToken, ct);
            return Ok(result);
        }

        [HttpPost("{cartId:int}/finalize")]
        [AllowAnonymous]
        public async Task<ActionResult<FinalizeResponseDto>> Finalize(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.FinalizeAsync(cartId, CustomerId, GuestToken, ct);
            await TrySendOrderDocumentsAsync(result.OrderId, ct);
            return Ok(result);
        }

        private async Task TrySendOrderDocumentsAsync(int orderId, CancellationToken ct)
        {
            var order = await _db.Orders
                .Include(o => o.BillingDetails)
                .Include(o => o.InvoiceDocument)
                .Include(o => o.Items)
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order is null)
                return;

            var toEmail = await ResolveReceiptEmailAsync(order, ct);
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var invoiceDocument = order.BillingDetails?.InvoiceStatus is InvoiceStatus.Requested or InvoiceStatus.Issued or InvoiceStatus.Sent
                ? await EnsureInvoiceDocumentAsync(order, ct)
                : order.InvoiceDocument;

            if (order.BillingDetails?.InvoiceStatus is InvoiceStatus.Requested or InvoiceStatus.Issued)
            {
                order.BillingDetails.InvoiceStatus = InvoiceStatus.Sent;
                order.BillingDetails.InvoiceIssuedAt ??= DateTime.UtcNow;
                order.BillingDetails.InvoiceSentAt = DateTime.UtcNow;
            }

            var emailModel = await _orderEmails.ComposeAsync(order, invoiceDocument, ct);

            var attachments = invoiceDocument is null
                ? null
                : new[]
                {
                    new EmailAttachment
                    {
                        FileName = invoiceDocument.FileName,
                        ContentType = invoiceDocument.ContentType,
                        ContentBytes = invoiceDocument.PdfBytes
                    }
                };

            await _email.SendAsync(
                toEmail,
                emailModel.Subject,
                OrderSummaryEmailBuilder.Build(emailModel),
                attachments,
                ct);

            order.ReceiptSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        private async Task<string?> ResolveReceiptEmailAsync(Order order, CancellationToken ct)
        {
            var toEmail = (order.ReceiptEmail ?? order.BillingDetails?.ReceiptEmail)?.Trim();
            if (!string.IsNullOrWhiteSpace(toEmail))
                return toEmail;

            if (string.IsNullOrWhiteSpace(order.CustomerId))
                return null;

            var accountEmail = await _db.Users
                .Where(u => u.Id == order.CustomerId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(accountEmail))
                return null;

            order.ReceiptEmail = accountEmail.Trim();
            return order.ReceiptEmail;
        }

        private async Task<OrderInvoiceDocument> EnsureInvoiceDocumentAsync(Order order, CancellationToken ct)
        {
            var existing = order.InvoiceDocument
                ?? await _db.OrderInvoiceDocuments.FirstOrDefaultAsync(x => x.OrderId == order.Id, ct);

            if (existing is not null)
            {
                order.InvoiceDocument = existing;
                await RefreshLegacyInvoicePdfAsync(order, existing, ct);
                return existing;
            }

            var generatedAt = DateTime.UtcNow;
            var invoiceNumber = $"INV-{generatedAt:yyyyMMdd}-{order.Id}";
            var itemNames = await LoadMenuItemNamesAsync(order, ct);
            var document = new OrderInvoiceDocument
            {
                OrderId = order.Id,
                InvoiceNumber = invoiceNumber,
                FileName = $"{invoiceNumber}.pdf",
                ContentType = "application/pdf",
                GeneratedAt = generatedAt,
                PdfBytes = InvoicePdfBuilder.Build(new
                {
                    InvoiceNumber = invoiceNumber,
                    OrderId = order.Id,
                    CreatedAt = order.CreatedAt,
                    CustomerName = order.BillingDetails?.CustomerType == BillingCustomerType.Company
                        ? order.BillingDetails?.CompanyName ?? "Customer"
                        : order.BillingDetails?.PersonName ?? "Customer",
                    Address = string.Join(", ", new[]
                    {
                        order.BillingDetails?.BillingAddressLine1,
                        order.BillingDetails?.BillingAddressLine2,
                        order.BillingDetails?.BillingCity,
                        order.BillingDetails?.BillingPostalCode,
                        order.BillingDetails?.BillingCountry
                    }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    TaxId = order.BillingDetails?.TaxId ?? "-",
                    Subtotal = order.Subtotal,
                    DeliveryFee = order.DeliveryFee,
                    Total = order.Total,
                    Items = order.Items.Select(i => new
                    {
                        i.MenuItemId,
                        Name = itemNames.GetValueOrDefault(i.MenuItemId, $"Menu item #{i.MenuItemId}"),
                        i.Quantity,
                        i.UnitPrice,
                        i.Note,
                        LineTotal = i.UnitPrice * i.Quantity
                    }).ToList()
                })
            };

            _db.OrderInvoiceDocuments.Add(document);
            order.InvoiceDocument = document;
            await _db.SaveChangesAsync(ct);
            return document;
        }

        private async Task RefreshLegacyInvoicePdfAsync(Order order, OrderInvoiceDocument document, CancellationToken ct)
        {
            if (!ContainsLegacyMenuItemPlaceholder(document))
                return;

            var itemNames = await LoadMenuItemNamesAsync(order, ct);
            document.PdfBytes = InvoicePdfBuilder.Build(new
            {
                InvoiceNumber = document.InvoiceNumber,
                OrderId = order.Id,
                CreatedAt = order.CreatedAt,
                CustomerName = order.BillingDetails?.CustomerType == BillingCustomerType.Company
                    ? order.BillingDetails?.CompanyName ?? "Customer"
                    : order.BillingDetails?.PersonName ?? "Customer",
                Address = string.Join(", ", new[]
                {
                    order.BillingDetails?.BillingAddressLine1,
                    order.BillingDetails?.BillingAddressLine2,
                    order.BillingDetails?.BillingCity,
                    order.BillingDetails?.BillingPostalCode,
                    order.BillingDetails?.BillingCountry
                }.Where(x => !string.IsNullOrWhiteSpace(x))),
                TaxId = order.BillingDetails?.TaxId ?? "-",
                Subtotal = order.Subtotal,
                DeliveryFee = order.DeliveryFee,
                Total = order.Total,
                Items = order.Items.Select(i => new
                {
                    i.MenuItemId,
                    Name = itemNames.GetValueOrDefault(i.MenuItemId, $"Menu item #{i.MenuItemId}"),
                    i.Quantity,
                    i.UnitPrice,
                    i.Note,
                    LineTotal = i.UnitPrice * i.Quantity
                }).ToList()
            });
            document.ContentType = "application/pdf";
            document.GeneratedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        private static bool ContainsLegacyMenuItemPlaceholder(OrderInvoiceDocument document)
            => Encoding.ASCII.GetString(document.PdfBytes).Contains("MenuItem #", StringComparison.Ordinal);

        private async Task<Dictionary<int, string>> LoadMenuItemNamesAsync(Order order, CancellationToken ct)
        {
            var ids = order.Items.Select(item => item.MenuItemId).Distinct().ToArray();
            return await _db.MenuItems
                .Where(menuItem => ids.Contains(menuItem.Id))
                .Select(menuItem => new
                {
                    menuItem.Id,
                    Name = menuItem.Translations
                        .OrderBy(translation => translation.Culture == "pl-PL" ? 0 : 1)
                        .Select(translation => translation.Name)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(item => item.Id, item => item.Name ?? $"Menu item #{item.Id}", ct);
        }
    }
}
