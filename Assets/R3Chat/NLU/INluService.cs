using R3Chat.NLU;
using System.Threading;
using System.Threading.Tasks;

public interface INluService
{
    Task<NluPacket> ExtractAsync(int turnId, string userText, CancellationToken ct);
}