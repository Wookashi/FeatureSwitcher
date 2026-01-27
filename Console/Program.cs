using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation;
using Wookashi.FeatureSwitcher.Console;

// var featureCollection = new Faker<ApplicationFeature>()
//         .CustomInstantiator(f => new ApplicationFeature(
//             $"{f.Hacker.Noun()}{f.Random.Number(1, 50)}",
//             f.Random.Bool(),
//             new Uri(f.Internet.Url())
//         ))
//         .Generate(10)
//         .Cast<IFeatureStateModel>()
//         .ToList();

var featureCollection = new List<IFeatureStateModel>
{
    new ApplicationFeature("Foo", true, new Uri("https://www.foo.pl")),
    new ApplicationFeature("Bar", false, new Uri("https://www.bar.pl")),
    new ApplicationFeature("Test", true, new Uri("https://www.test.pl")),
    new ApplicationFeature("Function one", false, new Uri("https://www.functionone.pl")),
    new ApplicationFeature("Function two", true, new Uri("https://www.functiontwo.pl")),
    new ApplicationFeature("Some other function", false, new Uri("https://www.sof.pl")),
    new ApplicationFeature("Another test", true, new Uri("https://www.at.pl")),
    new ApplicationFeature("last function", false, new Uri("https://www.lf.pl")),
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
var environmentName = Prompt("Environment name", "docker");
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
        var table = new Table().Centered();
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
        AnsiConsole.Write(table);
    }
}
catch (NodeUnreachableException exception)
{
    Console.WriteLine("Node unreachable Exception");
    Console.WriteLine("Exception message: " + exception.Message);
}