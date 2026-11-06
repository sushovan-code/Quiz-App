using Microsoft.Extensions.Options;

namespace BidQuiz.Api;

public static class OptionsRegistration
{
    /// <summary>Binds a configuration section, checks its data annotations, and fails at startup if they don't hold.</summary>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class =>
        services.AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
