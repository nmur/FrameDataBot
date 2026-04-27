using FrameData.Bot.Formatting;
using FrameData.Shared.Contracts;

namespace FrameData.Bot.Commands;

public sealed class MoveCommandHandler
{
    private readonly MoveCommandParser _parser;
    private readonly MoveResponseFormatter _formatter;

    public MoveCommandHandler(MoveCommandParser parser, MoveResponseFormatter formatter)
    {
        _parser = parser;
        _formatter = formatter;
    }

    public string Handle(string[] args, Func<string, string, Task<(MoveQueryResponse? Response, MoveAmbiguousResponse? Ambiguous, ErrorResponse? Error)>> query)
    {
        var parsed = _parser.Parse(args);
        if (!parsed.IsValid)
        {
            return parsed.Error ?? "Invalid command";
        }

        var result = query(parsed.Character!, parsed.Move!).GetAwaiter().GetResult();

        if (result.Response is not null)
        {
            return _formatter.FormatSuccess(result.Response);
        }

        if (result.Ambiguous is not null)
        {
            return _formatter.FormatAmbiguous(result.Ambiguous);
        }

        return _formatter.FormatError(result.Error ?? new ErrorResponse { Code = "error", Message = "Unknown error" });
    }
}
