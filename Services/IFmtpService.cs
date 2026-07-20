using RawV.Models;

namespace RawV.Services;

public interface IFmtpService
{
    Task<int> RunAsync(
        string localDirectory,
        string mtpPath,
        IProgress<FmtpEvent> progress,
        CancellationToken cancellationToken);
}
