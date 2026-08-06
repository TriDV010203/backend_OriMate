using Microsoft.Extensions.Options;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Infrastructure.Options;

namespace OrigamiPlatform.Infrastructure.Services;

public class BankAccountInfoProvider : IBankAccountInfoProvider
{
    private readonly BankAccountOptions _options;

    public BankAccountInfoProvider(IOptions<BankAccountOptions> options)
        => _options = options.Value;

    public string AccountNumber => _options.AccountNumber;
    public string BankName => _options.BankName;
    public string BankBin => _options.BankBin;
    public string AccountHolderName => _options.AccountHolderName;
}
