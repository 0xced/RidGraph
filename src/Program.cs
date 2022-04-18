var compatibleRuntimes = args;
var errorConsole = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) });

try
{
    var runtimeJsonUri = new Uri("https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/Microsoft.NETCore.Platforms/src/runtime.json");
    using var httpClient = new HttpClient();
    var runtimeJsonStream = await httpClient.GetStreamAsync(runtimeJsonUri);
    var runtimeGraph = JsonRuntimeFormat.ReadRuntimeGraph(runtimeJsonStream);
    var edges = from runtime in runtimeGraph.Runtimes.Values
                from inheritedRuntime in runtime.InheritedRuntimes
                where compatibleRuntimes.Length == 0 || compatibleRuntimes.Any(e => runtimeGraph.AreCompatible(inheritedRuntime, e))
                select new DotEdge(runtime.RuntimeIdentifier, inheritedRuntime);

    var filterDescription = compatibleRuntimes.Any() ? $" ({string.Join(" ", compatibleRuntimes)})" : "";
    var linkStyle = new DotStyledFont(DotFontStyles.Underline, System.Drawing.Color.Blue);
    var dotGraph = new DotGraph
    {
        Label = new DotHtmlBuilder().AppendStyledText(".NET RID Catalog", linkStyle).AppendText(filterDescription).Build(),
        Hyperlink = { Url = "https://docs.microsoft.com/en-us/dotnet/core/rid-catalog" },
        Layout = { Direction = DotLayoutDirection.BottomToTop },
    };
    dotGraph.Edges.AddRange(edges);

    if (dotGraph.Edges.Count == 0)
    {
        throw new ApplicationException($"No graph was constructed. Make sure that the specified filter{filterDescription} matches at least one runtime identifier in the graph.");
    }

    var input = PipeSource.Create(stream =>
    {
        using var writer = new StreamWriter(stream);
        dotGraph.Build(writer);
    });
    var dot = Cli.Wrap("dot").WithArguments(new[] { "-Tsvg" }).WithStandardErrorPipe(PipeTarget.ToDelegate(line => errorConsole.WriteLine(line, new Style(Color.DarkOrange))));
    var output = PipeTarget.ToDelegate(AnsiConsole.WriteLine);

    await (input | dot | output).ExecuteAsync();
}
catch (Exception exception)
{
    errorConsole.WriteException(exception);
}
