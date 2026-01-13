using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation;
using Wookashi.FeatureSwitcher.Console;

var featureCollection = new Faker<ApplicationFeature>()
        .CustomInstantiator(f => new ApplicationFeature(
            $"{f.Hacker.Noun()}{f.Random.Number(1, 50)}",
            f.Random.Bool(),
            new Uri(f.Internet.Url())
        ))
        .Generate(10)
        .Cast<IFeatureStateModel>()
        .ToList();

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

var applicationName = Prompt("Application name", "Console");
var environmentName = Prompt("Environment name", "testEnv");
var environmentNodeAddress = PromptUri("Node address", new Uri("http://localhost:5216"));

var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
var featureManagerBuilder = new FeatureManagerBuilder(new FeatureSwitcherBasicClientConfiguration(
        applicationName: applicationName,
        environmentName: environmentName,
        environmentNodeAddress: environmentNodeAddress))
    .AddFeatures(featureCollection)
    .AddHttpClientFactory(httpClientFactory!);

try
{
    var featureManager = await featureManagerBuilder.BuildAsync();
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