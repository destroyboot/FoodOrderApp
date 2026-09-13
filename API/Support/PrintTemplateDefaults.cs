namespace API.Support;

public sealed record PrintTemplateDefinition(
    string Code,
    string Name,
    string Description,
    string DocumentKind,
    string HtmlTemplate,
    IReadOnlyList<string> AvailableTokens);

public static class PrintTemplateDefaults
{
    public const string KindReport = "Report";
    public const string KindOrder = "Order";
    public const string KindInvoice = "Invoice";
    public const string KindEmail = "Email";

    public const string SalesSummaryReport = "report.sales-summary";
    public const string CustomerFrequencyReport = "report.customer-frequency";
    public const string CustomerMixReport = "report.customer-mix";
    public const string MenuPopularityReport = "report.menu-popularity";
    public const string UserActivityReport = "report.user-activity";
    public const string OrderSummaryPdf = "order.summary.pdf";
    public const string OrderSummaryEmail = "order.summary.email";
    public const string InvoicePdf = "order.invoice.pdf";
    public const string OrderStatusEmail = "order.status.email";
    public const string AccountRegistrationCodeEmail = "account.registration-code.email";
    public const string AccountActivatedEmail = "account.activated.email";
    public const string AccountPasswordResetEmail = "account.password-reset.email";
    public const string AccountPasswordChangedEmail = "account.password-changed.email";
    public const string AccountEmailChangeCodeEmail = "account.email-change-code.email";
    public const string AccountEmailChangedEmail = "account.email-changed.email";
    public const string AccountDeletionCodeEmail = "account.deletion-code.email";

    private static readonly string[] ReportTokens =
    {
        "report.title",
        "report.generatedAt",
        "report.dateFrom",
        "report.dateTo",
        "report.summaryHtml",
        "report.tableHtml"
    };

    private static readonly string[] OrderTokens =
    {
        "order.id",
        "order.number",
        "order.createdAt",
        "order.status",
        "order.type",
        "order.paymentMethod",
        "order.paymentStatus",
        "order.subtotal",
        "order.deliveryFee",
        "order.total",
        "restaurant.name",
        "customer.name",
        "customer.email",
        "invoice.number",
        "itemsTableHtml"
    };

    private static readonly string[] OrderSummaryEmailTokens =
    {
        "title",
        "labels.date",
        "labels.restaurant",
        "labels.status",
        "labels.type",
        "labels.table",
        "labels.subtotal",
        "labels.deliveryFee",
        "labels.total",
        "labels.invoice",
        "order.number",
        "order.createdAt",
        "order.status",
        "order.type",
        "order.tableNumber",
        "order.subtotal",
        "order.deliveryFee",
        "order.total",
        "restaurant.name",
        "invoice.number",
        "thankYou",
        "itemsTableHtml"
    };

    private static readonly string[] StatusEmailTokens =
    {
        "order.id",
        "order.number",
        "oldStatus",
        "newStatus"
    };

    private static readonly string[] AccountCodeEmailTokens =
    {
        "account.email",
        "code",
        "expiresAt"
    };

    private static readonly string[] AccountLinkEmailTokens =
    {
        "account.email",
        "link"
    };

    private static readonly string[] AccountSimpleEmailTokens =
    {
        "account.email"
    };

    public static IReadOnlyList<PrintTemplateDefinition> All { get; } = new List<PrintTemplateDefinition>
    {
        Report(SalesSummaryReport, "Sales summary report", "Default PDF template for the sales summary report."),
        Report(CustomerFrequencyReport, "Customer frequency report", "Default PDF template for customer frequency analytics."),
        Report(CustomerMixReport, "Customer mix report", "Default PDF template for registered vs anonymous order analytics."),
        Report(MenuPopularityReport, "Menu popularity report", "Default PDF template for menu popularity analytics."),
        Report(UserActivityReport, "User activity report", "Default PDF template for user activity analytics."),
        new(
            OrderSummaryPdf,
            "Order summary PDF",
            "Default printable order summary.",
            KindOrder,
            """
            <h1>Order summary #{{order.number}}</h1>
            <p><strong>Restaurant:</strong> {{restaurant.name}}</p>
            <p><strong>Created:</strong> {{order.createdAt}}</p>
            <p><strong>Status:</strong> {{order.status}}</p>
            <p><strong>Type:</strong> {{order.type}}</p>
            {{{itemsTableHtml}}}
            <p><strong>Subtotal:</strong> {{order.subtotal}}</p>
            <p><strong>Delivery fee:</strong> {{order.deliveryFee}}</p>
            <h2>Total: {{order.total}}</h2>
            """,
            OrderTokens),
        new(
            OrderSummaryEmail,
            "Order summary email",
            "Default email template sent with order confirmation and invoice attachment.",
            KindEmail,
            """
            <h2>{{title}}</h2>
            <p>
              <strong>{{labels.date}}:</strong> {{order.createdAt}}<br>
              <strong>{{labels.restaurant}}:</strong> {{restaurant.name}}<br>
              <strong>{{labels.status}}:</strong> {{order.status}}<br>
              <strong>{{labels.type}}:</strong> {{order.type}}<br>
              {{#if order.tableNumber}}<strong>{{labels.table}}:</strong> {{order.tableNumber}}<br>{{/if}}
            </p>
            {{{itemsTableHtml}}}
            <hr>
            <p>
              <strong>{{labels.subtotal}}:</strong> {{order.subtotal}}<br>
              <strong>{{labels.deliveryFee}}:</strong> {{order.deliveryFee}}<br>
              <strong>{{labels.total}}:</strong> {{order.total}}
            </p>
            {{#if invoice.number}}
            <p><strong>{{labels.invoice}}:</strong> {{invoice.number}}</p>
            {{/if}}
            <p>{{thankYou}}</p>
            """,
            OrderSummaryEmailTokens),
        new(
            InvoicePdf,
            "Invoice PDF",
            "Default invoice PDF template.",
            KindInvoice,
            """
            <h1>Invoice {{invoice.number}}</h1>
            <p><strong>Order:</strong> #{{order.number}}</p>
            <p><strong>Restaurant:</strong> {{restaurant.name}}</p>
            <p><strong>Buyer:</strong> {{billing.name}}</p>
            {{{itemsTableHtml}}}
            <h2>Total: {{order.total}}</h2>
            """,
            OrderTokens.Concat(new[] { "invoice.number", "billing.name", "billing.taxId", "billing.address" }).ToList()),
        new(
            OrderStatusEmail,
            "Order status email",
            "Default email template for order status changes.",
            KindEmail,
            """
            <h1>Order #{{order.number}} status update</h1>
            <p>Your order status changed from <strong>{{oldStatus}}</strong> to <strong>{{newStatus}}</strong>.</p>
            <p>Thank you!</p>
            """,
            StatusEmailTokens),
        new(
            AccountRegistrationCodeEmail,
            "Account registration confirmation email",
            "Default email template with the code used to confirm a newly registered account.",
            KindEmail,
            """
            <p>Your confirmation code is:</p>
            <h2 style='letter-spacing:2px;'>{{code}}</h2>
            <p>This code is valid for 20 minutes.</p>
            """,
            AccountCodeEmailTokens),
        new(
            AccountActivatedEmail,
            "Account activated email",
            "Default email template sent after successful account activation.",
            KindEmail,
            """
            <p>Your account is now active. You can log in and start ordering.</p>
            """,
            AccountSimpleEmailTokens),
        new(
            AccountPasswordResetEmail,
            "Password reset email",
            "Default email template with the password reset link.",
            KindEmail,
            """
            <p>Click the link below to reset your password:</p>
            <p><a href="{{link}}">Reset password</a></p>
            <p>This link is valid for a limited time.</p>
            <p>If you didn’t request this, ignore this message.</p>
            """,
            AccountLinkEmailTokens),
        new(
            AccountPasswordChangedEmail,
            "Password changed email",
            "Default email template sent after a successful password change.",
            KindEmail,
            """
            <p>Your password was changed successfully.</p>
            """,
            AccountSimpleEmailTokens),
        new(
            AccountEmailChangeCodeEmail,
            "Email change confirmation email",
            "Default email template with the code used to confirm an email address change.",
            KindEmail,
            """
            <p>Use this code to confirm your email change:</p>
            <h2 style='letter-spacing:2px;'>{{code}}</h2>
            <p>This code is valid for 20 minutes.</p>
            """,
            AccountCodeEmailTokens),
        new(
            AccountEmailChangedEmail,
            "Email changed email",
            "Default email template sent after a successful email address change.",
            KindEmail,
            """
            <p>Your email address has been updated successfully.</p>
            """,
            AccountSimpleEmailTokens),
        new(
            AccountDeletionCodeEmail,
            "Account deletion confirmation email",
            "Default email template with the code used to confirm account removal.",
            KindEmail,
            """
            <p>Use this code to confirm account removal:</p>
            <h2 style='letter-spacing:2px;'>{{code}}</h2>
            <p>This code is valid for 20 minutes.</p>
            """,
            AccountCodeEmailTokens)
    };

    public static PrintTemplateDefinition GetRequired(string code)
        => All.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown print template code: {code}");

    private static PrintTemplateDefinition Report(string code, string name, string description)
        => new(
            code,
            name,
            description,
            KindReport,
            """
            <h1>{{report.title}}</h1>
            <p><strong>Generated:</strong> {{report.generatedAt}}</p>
            <p><strong>Date range:</strong> {{report.dateFrom}} - {{report.dateTo}}</p>
            <h2>Summary</h2>
            {{{report.summaryHtml}}}
            <h2>Data</h2>
            {{{report.tableHtml}}}
            """,
            ReportTokens);
}
