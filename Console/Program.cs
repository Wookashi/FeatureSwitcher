using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;
using Wookashi.FeatureSwitcher.Client.Implementation;


// Description and Links are optional; the Manager shows them when the feature is opened.
var featureCollection = new List<IFeatureStateModel>
{
    new FeatureStateModel("Foo", true)
    {
        Description = "Shows the new Foo panel on the dashboard.",
        Links = ["https://www.foo.pl"],
    },
    new FeatureStateModel("Bar", false)
    {
        Description = """
            Switches Bar to the new pricing engine.

            When OFF:
            - prices come from the legacy engine
            - cached prices are kept, no restart needed
            """,
        Links =
        [
            new("Specification", "https://www.bar.pl"),
            "https://www.bar.pl/runbook",
        ],
    },
    new FeatureStateModel("Test", true) { Links = ["https://www.test.pl"] },
    new FeatureStateModel("Function one", false),
    new FeatureStateModel("Function two", true)
    {
        Description = "Kill switch for function two. Dashboard: https://www.functiontwo.pl/dashboard",
    },
    new FeatureStateModel("Some other function", false),
    new FeatureStateModel("Another test", true),
    new FeatureStateModel("last function", false),
};

var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();

static string Prompt(string label, string defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var input = Console.ReadLine();
    return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
}

static Uri PromptUri(string label, Uri defaultValue)
{
    while (true)
    {
        var input = Prompt(label, defaultValue.ToString());

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return uri;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Invalid URI, try again.");
        Console.ResetColor();
    }
}

var applicationName = Prompt("Application name", "TestApp");
var environmentName = Prompt("Environment name", "Development");
var environmentNodeAddress = PromptUri("Node address", new Uri("http://localhost:8081"));

var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
var featureManager = new FeatureManager(
    applicationName: applicationName,
    environmentName: environmentName,
    nodeAddress: environmentNodeAddress,
    features: featureCollection,
    httpClientFactory: httpClientFactory);

try
{
    await featureManager.RegisterFeaturesOnNodeAsync();
    while (true)
    {
        Thread.Sleep(1000);
        var table = new Table();
        table.AddColumn("Feature name");
        table.AddColumn("State");


        foreach (var featureName in featureCollection.Select(feature => feature.Name))
        {
            table.AddRow(featureName, featureManager.IsFeatureEnabledAsync(featureName).Result ? "[green]Yes[/]" : "[red]No[/]");
        }

        table.Border = TableBorder.Rounded;
        table.BorderColor(Color.Blue);
        table.Title = new TableTitle("Feature toggles");
        table.Caption = new TableTitle($"Last updated {DateTime.Now.ToLongTimeString()}");
        Console.Clear();
        AnsiConsole.Write(new Text($"env:{environmentName}, appName:{applicationName}, node:{environmentNodeAddress}").Centered());
        AnsiConsole.Write(Align.Center(table));
    }
}
catch (NodeUnreachableException exception)
{
    Console.WriteLine("Node unreachable Exception");
    Console.WriteLine("Exception message: " + exception.Message);
}