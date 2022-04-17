namespace RidGraph;

public class DotGraphPipeSource : PipeSource
{
    private readonly DotGraph _graph;

    public DotGraphPipeSource(DotGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        using var streamWriter = new StreamWriter(destination, leaveOpen: true);
        _graph.Build(streamWriter);
        return Task.CompletedTask;
    }
}
