using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class VoiceOrchestrationService : IVoiceOrchestrationService
{
    private readonly VoiceIntentExtractor _extractor;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILedgerService _ledgerService;
    private readonly IExpenseService _expenseService;
    private readonly ISaleService _saleService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public VoiceOrchestrationService(
        VoiceIntentExtractor extractor,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        ILedgerService ledgerService,
        IExpenseService expenseService,
        ISaleService saleService,
        IStringLocalizer<SharedResource> localizer
    )
    {
        _extractor = extractor;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _ledgerService = ledgerService;
        _expenseService = expenseService;
        _saleService = saleService;
        _localizer = localizer;
    }

    public async Task<VoiceCommandResponse> ProcessVoiceCommandAsync(
        byte[] audioBytes,
        string mimeType,
        Guid businessId,
        CancellationToken ct = default
    )
    {
        var intentResult = await _extractor.ExtractAsync(audioBytes, mimeType, ct);

        if (intentResult is null || intentResult.Intent == VoiceIntent.Unknown)
        {
            return Fail(VoiceIntent.Unknown, _localizer["Voice_UnknownCommand"]);
        }

        return intentResult.Intent switch
        {
            VoiceIntent.RecordPayment => await HandleRecordPaymentAsync(
                intentResult,
                businessId,
                ct
            ),
            VoiceIntent.CreateExpense => await HandleCreateExpenseAsync(
                intentResult,
                businessId,
                ct
            ),
            VoiceIntent.CreateSale => await HandleCreateSaleAsync(
                intentResult,
                businessId,
                forceUdhar: false,
                ct
            ),
            VoiceIntent.AddUdhar => await HandleAddUdharAsync(intentResult, businessId, ct),
            _ => Fail(intentResult.Intent, _localizer["Voice_UnsupportedCommand"]),
        };
    }

    private async Task<VoiceCommandResponse> HandleAddUdharAsync(
        VoiceIntentResult intent,
        Guid businessId,
        CancellationToken ct
    )
    {
        var hasProducts = intent.Items is not null && intent.Items.Count > 0;

        if (hasProducts)
        {
            return await HandleCreateSaleAsync(intent, businessId, forceUdhar: true, ct);
        }

        if (string.IsNullOrWhiteSpace(intent.CustomerName))
            return Fail(intent.Intent, _localizer["Voice_NoCustomerName"]);

        if (intent.Amount is null || intent.Amount <= 0)
            return Fail(intent.Intent, _localizer["Voice_NoValidUdharAmount"]);

        var matchedCustomer = await ResolveCustomerAsync(intent.CustomerName, businessId);

        if (matchedCustomer is null)
            return Fail(intent.Intent, _localizer["Voice_CustomerNotFound", intent.CustomerName]);

        if (matchedCustomer.Ambiguous)
            return Fail(intent.Intent, _localizer["Voice_MultipleCustomersFound", intent.CustomerName]);

        try
        {
            var request = new AddUdharRequest
            {
                CustomerId = matchedCustomer.Customer!.Id,
                Amount = intent.Amount.Value,
                Notes = _localizer["Voice_RecordedViaVoice"],
            };

            var entry = await _ledgerService.AddUdharAsync(businessId, request);

            var message = _localizer[
                "Voice_UdharRecorded",
                intent.Amount.Value.ToString("N0"),
                matchedCustomer.Customer.Name
            ].Value
                + (matchedCustomer.AutoAccepted ? $" (auto-matched from '{intent.CustomerName}')" : "");

            return new VoiceCommandResponse
            {
                Intent = intent.Intent,
                Success = true,
                Message = message,
                Data = entry,
            };
        }
        catch (Exception ex)
            when (ex
                    is KhataFlow.Core.Exceptions.DomainException
                        or FluentValidation.ValidationException
                        or KhataFlow.Core.Exceptions.NotFoundException
            )
        {
            return Fail(intent.Intent, ex.Message);
        }
    }

    private async Task<VoiceCommandResponse> HandleCreateSaleAsync(
        VoiceIntentResult intent,
        Guid businessId,
        bool forceUdhar,
        CancellationToken ct
    )
    {
        if (intent.Items is null || intent.Items.Count == 0)
            return Fail(intent.Intent, _localizer["Voice_NoProductsUnderstood"]);

        var wantsUdhar =
            forceUdhar
            || string.Equals(intent.PaymentMethod, "Udhar", StringComparison.OrdinalIgnoreCase);

        Guid? customerId = null;
        var autoCorrectionNotes = new List<string>();

        if (!string.IsNullOrWhiteSpace(intent.CustomerName))
        {
            var matchedCustomer = await ResolveCustomerAsync(intent.CustomerName, businessId);

            if (matchedCustomer is null)
                return Fail(
                    intent.Intent,
                    _localizer["Voice_CustomerNotFound", intent.CustomerName]
                );

            if (matchedCustomer.Ambiguous)
                return Fail(
                    intent.Intent,
                    _localizer["Voice_MultipleCustomersFound", intent.CustomerName]
                );

            customerId = matchedCustomer.Customer!.Id;

            if (matchedCustomer.AutoAccepted)
                autoCorrectionNotes.Add($"'{intent.CustomerName}' → {matchedCustomer.Customer.Name}");
        }

        if (wantsUdhar && customerId is null)
            return Fail(intent.Intent, _localizer["Voice_CustomerRequiredForUdhar"]);

        var (products, _) = await _productRepository.GetPagedAsync(
            businessId,
            pageNumber: 1,
            pageSize: 1000
        );

        var resolvedItems = new List<SaleItemRequest>();

        foreach (var spokenItem in intent.Items)
        {
            var match = ResolveProduct(spokenItem.ProductName, products);

            if (match is null)
                return Fail(
                    intent.Intent,
                    _localizer["Voice_ProductNotFound", spokenItem.ProductName]
                );

            if (match.Ambiguous)
                return Fail(
                    intent.Intent,
                    _localizer["Voice_MultipleProductsFound", spokenItem.ProductName]
                );

            if (match.Product is null)
            {
                var message =
                    match.Suggestions.Count > 0
                        ? _localizer[
                            "Voice_ProductNotFoundWithSuggestions",
                            spokenItem.ProductName,
                            string.Join(", ", match.Suggestions)
                        ]
                        : _localizer["Voice_ProductNotFound", spokenItem.ProductName];

                return Fail(intent.Intent, message);
            }

            if (match.AutoAccepted)
                autoCorrectionNotes.Add($"'{spokenItem.ProductName}' → {match.Product.ProductName}");

            var quantity = (int)spokenItem.Quantity;
            if (quantity <= 0)
                quantity = 1;

            resolvedItems.Add(new SaleItemRequest(match.Product!.Id, quantity));
        }

        try
        {
            var request = new SaleAddRequest(
                CustomerId: customerId,
                PaymentStatus: wantsUdhar ? PaymentStatus.Udhar : PaymentStatus.Paid,
                Note: _localizer["Voice_RecordedViaVoice"],
                Items: resolvedItems
            );

            var sale = await _saleService.AddSaleAsync(request, businessId);

            var baseMessage = wantsUdhar
                ? _localizer["Voice_UdharSaleRecorded", resolvedItems.Count].Value
                : _localizer["Voice_SaleRecorded", resolvedItems.Count].Value;

            var message = autoCorrectionNotes.Count > 0
                ? $"{baseMessage} (auto-matched: {string.Join(", ", autoCorrectionNotes)})"
                : baseMessage;

            return new VoiceCommandResponse
            {
                Intent = intent.Intent,
                Success = true,
                Message = message,
                Data = sale,
            };
        }
        catch (Exception ex)
            when (ex is FluentValidation.ValidationException or KeyNotFoundException)
        {
            return Fail(intent.Intent, ex.Message);
        }
    }

    private async Task<VoiceCommandResponse> HandleRecordPaymentAsync(
        VoiceIntentResult intent,
        Guid businessId,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(intent.CustomerName))
            return Fail(intent.Intent, _localizer["Voice_NoCustomerName"]);

        if (intent.Amount is null || intent.Amount <= 0)
            return Fail(intent.Intent, _localizer["Voice_NoValidPaymentAmount"]);

        var matchedCustomer = await ResolveCustomerAsync(intent.CustomerName, businessId);

        if (matchedCustomer is null)
            return Fail(intent.Intent, _localizer["Voice_CustomerNotFound", intent.CustomerName]);

        if (matchedCustomer.Ambiguous)
            return Fail(
                intent.Intent,
                _localizer["Voice_MultipleCustomersFound", intent.CustomerName]
            );

        try
        {
            var request = new RecordPaymentRequest
            {
                CustomerId = matchedCustomer.Customer!.Id,
                Amount = intent.Amount.Value,
                Notes = _localizer["Voice_RecordedViaVoice"],
            };

            var entry = await _ledgerService.RecordPaymentAsync(businessId, request);

            var message = _localizer[
                "Voice_PaymentRecorded",
                intent.Amount.Value.ToString("N0"),
                matchedCustomer.Customer.Name
            ].Value
                + (matchedCustomer.AutoAccepted ? $" (auto-matched from '{intent.CustomerName}')" : "");

            return new VoiceCommandResponse
            {
                Intent = intent.Intent,
                Success = true,
                Message = message,
                Data = entry,
            };
        }
        catch (Exception ex)
            when (ex
                    is KhataFlow.Core.Exceptions.DomainException
                        or FluentValidation.ValidationException
                        or KhataFlow.Core.Exceptions.NotFoundException
            )
        {
            return Fail(intent.Intent, ex.Message);
        }
    }

    private async Task<VoiceCommandResponse> HandleCreateExpenseAsync(
        VoiceIntentResult intent,
        Guid businessId,
        CancellationToken ct
    )
    {
        if (intent.Amount is null || intent.Amount <= 0)
            return Fail(intent.Intent, _localizer["Voice_NoValidExpenseAmount"]);

        if (
            !Enum.TryParse<ExpenseCategory>(
                intent.ExpenseCategory,
                ignoreCase: true,
                out var category
            )
        )
            category = ExpenseCategory.Miscellaneous;

        try
        {
            var request = new ExpenseAddRequest
            {
                Title = !string.IsNullOrWhiteSpace(intent.Description)
                    ? intent.Description
                    : (intent.ExpenseCategory ?? "Voice expense"),
                Amount = intent.Amount.Value,
                Category = category,
                Note = _localizer["Voice_RecordedViaVoice"],
            };

            var expense = await _expenseService.AddExpenseAsync(businessId, request);

            return new VoiceCommandResponse
            {
                Intent = intent.Intent,
                Success = true,
                Message = _localizer[
                    "Voice_ExpenseRecorded",
                    intent.Amount.Value.ToString("N0"),
                    category
                ],
                Data = expense,
            };
        }
        catch (Exception ex) when (ex is FluentValidation.ValidationException)
        {
            return Fail(intent.Intent, ex.Message);
        }
    }

    private async Task<CustomerMatchResult?> ResolveCustomerAsync(string spokenName, Guid businessId)
    {
        var customers = await _customerRepository.GetByBusinessIdAsync(businessId);
        var customerList = customers.ToList();

        if (customerList is { Count: 0 }) return null;

        var normalized = NormalizeForMatch(spokenName);
        var tightSpoken = normalized.Replace(" ", "");

        var exact = customerList
            .Where(c => NormalizeForMatch(c.Name) == normalized)
            .ToList();

        if (exact is [var onlyExactMatch]) return new CustomerMatchResult { Customer = onlyExactMatch };
        if (exact.Count > 1) return new CustomerMatchResult { Ambiguous = true };

        // --- Tier 2: match ignoring spaces entirely ---
        var tightMatches = customerList
            .Where(c => NormalizeForMatch(c.Name).Replace(" ", "") == tightSpoken)
            .ToList();

        if (tightMatches is [var onlyTightMatch]) return new CustomerMatchResult { Customer = onlyTightMatch };
        if (tightMatches.Count > 1) return new CustomerMatchResult { Ambiguous = true };

        const double similarityThreshold = 0.72;
        const double autoAcceptThreshold = 0.55;
        const double autoAcceptMargin = 0.20;

        var scored = customerList
            .Select(c => new
            {
                Customer = c,
                Score = SimilarityScore(tightSpoken, NormalizeForMatch(c.Name).Replace(" ", ""))
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var accepted = scored.Where(x => x.Score >= similarityThreshold).ToList();

        if (accepted is { Count: > 0 })
        {
            var topScore = accepted[0].Score;
            var topMatches = accepted.Where(x => Math.Abs(x.Score - topScore) < 0.01).ToList();

            return topMatches switch
            {
                [var onlyTopMatch] => new CustomerMatchResult { Customer = onlyTopMatch.Customer },
                _ => new CustomerMatchResult { Ambiguous = true }
            };
        }

        var runnerUpScore = scored.Count > 1 ? scored[1].Score : 0.0;

        if (scored[0].Score >= autoAcceptThreshold
            && scored[0].Score - runnerUpScore >= autoAcceptMargin)
        {
            return new CustomerMatchResult { Customer = scored[0].Customer, AutoAccepted = true };
        }

        return null;
    }

    private static ProductMatchResult? ResolveProduct(
        string spokenName,
        IEnumerable<Domain.Entities.Product> products
    )
    {
        var normalized = NormalizeForMatch(spokenName);
        var productList = products.ToList();

        if (productList is { Count: 0 })
            return null;

        var exact = productList.Where(p => NormalizeForMatch(p.ProductName) == normalized).ToList();

        if (exact is [var onlyExactMatch])
            return new ProductMatchResult { Product = onlyExactMatch };
        if (exact.Count > 1)
            return new ProductMatchResult { Ambiguous = true };

        var contains = productList
            .Where(p =>
            {
                var pn = NormalizeForMatch(p.ProductName);
                return pn.Contains(normalized) || normalized.Contains(pn);
            })
            .ToList();

        if (contains is [var onlyContainsMatch])
            return new ProductMatchResult { Product = onlyContainsMatch };
        if (contains.Count > 1)
            return new ProductMatchResult { Ambiguous = true };

        const double similarityThreshold = 0.65;
        const double autoAcceptThreshold = 0.45;
        const double autoAcceptMargin = 0.15;
        const double suggestionThreshold = 0.35;
        const int maxSuggestions = 3;

        var spokenTokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var allScored = productList
            .Select(p => new
            {
                Product = p,
                Score = TokenSimilarityScore(
                    spokenTokens,
                    NormalizeForMatch(p.ProductName)
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                ),
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var accepted = allScored.Where(x => x.Score >= similarityThreshold).ToList();

        if (accepted is { Count: > 0 })
        {
            var topScore = accepted[0].Score;
            var topMatches = accepted.Where(x => Math.Abs(x.Score - topScore) < 0.01).ToList();

            return topMatches switch
            {
                [var onlyTopMatch] => new ProductMatchResult { Product = onlyTopMatch.Product },
                _ => new ProductMatchResult { Ambiguous = true },
            };
        }

        var runnerUpScore = allScored.Count > 1 ? allScored[1].Score : 0.0;

        if (
            allScored[0].Score >= autoAcceptThreshold
            && allScored[0].Score - runnerUpScore >= autoAcceptMargin
        )
        {
            return new ProductMatchResult { Product = allScored[0].Product, AutoAccepted = true };
        }

        var suggestions = allScored
            .Where(x => x.Score >= suggestionThreshold)
            .Take(maxSuggestions)
            .Select(x => x.Product.ProductName)
            .ToList();

        return new ProductMatchResult { Product = null, Suggestions = suggestions };
    }

    private static double TokenSimilarityScore(string[] spokenTokens, string[] productTokens)
    {
        if (spokenTokens.Length == 0 || productTokens.Length == 0)
            return 0.0;

        double total = 0;

        foreach (var spoken in spokenTokens)
        {
            double best = 0;

            foreach (var product in productTokens)
            {
                var score = TokenScore(spoken, product);
                if (score > best)
                    best = score;
            }

            total += best;
        }

        return total / spokenTokens.Length;
    }

    private static double TokenScore(string a, string b)
    {
        if (a == b)
            return 1.0;
        if (a.Length == 0 || b.Length == 0)
            return 0.0;

        if (a.Contains(b) || b.Contains(a))
        {
            var shorter = Math.Min(a.Length, b.Length);
            var longer = Math.Max(a.Length, b.Length);
            return 0.7 + 0.3 * ((double)shorter / longer);
        }

        return SimilarityScore(a, b);
    }

    private static double SimilarityScore(string a, string b)
    {
        if (a == b)
            return 1.0;
        if (a.Length == 0 || b.Length == 0)
            return 0.0;

        var distance = LevenshteinDistance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        return 1.0 - ((double)distance / maxLen);
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var costs = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
            costs[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            costs[0] = i;
            var previousDiagonal = i - 1;

            for (var j = 1; j <= b.Length; j++)
            {
                var previousDiagonalSave = costs[j];
                costs[j] =
                    a[i - 1] == b[j - 1]
                        ? previousDiagonal
                        : 1 + Math.Min(previousDiagonal, Math.Min(costs[j], costs[j - 1]));
                previousDiagonal = previousDiagonalSave;
            }
        }

        return costs[b.Length];
    }

    private static string NormalizeForMatch(string input) =>
        string.Join(
                " ",
                input
                    .ToLowerInvariant()
                    .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            )
            .Trim();

    private static VoiceCommandResponse Fail(VoiceIntent intent, string message) =>
        new()
        {
            Intent = intent,
            Success = false,
            ErrorMessage = message,
        };

    private class CustomerMatchResult
    {
        public Domain.Entities.Customer? Customer { get; set; }
        public bool Ambiguous { get; set; }
        public bool AutoAccepted { get; set; }
    }

    private class ProductMatchResult
    {
        public Domain.Entities.Product? Product { get; set; }
        public bool Ambiguous { get; set; }
        public bool AutoAccepted { get; set; }
        public List<string> Suggestions { get; set; } = new();
    }
}