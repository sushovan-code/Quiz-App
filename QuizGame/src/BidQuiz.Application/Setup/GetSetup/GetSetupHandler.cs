using BidQuiz.Application.Abstractions.Persistence;

namespace BidQuiz.Application.Setup.GetSetup;

public sealed class GetSetupHandler(IUnitOfWorkFactory unitOfWork)
{
    public async Task<SetupResponse> HandleAsync(CancellationToken cancellationToken)
    {
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        return await SetupReader.ReadAsync(work, cancellationToken);
    }
}
