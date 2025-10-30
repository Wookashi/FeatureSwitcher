using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation;
using Wookashi.FeatureSwitcher.Console;


var featureCollection = new List<IFeatureStateModel>
{
    new ApplicationFeature("Foo", true, new Uri("https://www.wp.pl")),
    new ApplicationFeature("Bar", false, new Uri("https://www.wp.pl")),
    new ApplicationFeature("Baz", true, new Uri("https://www.wp.pl")),
    new ApplicationFeature("Qux", false, new Uri("https://www.wp.pl")),
    new ApplicationFeature("Quu1", true, new Uri("https://www.wp.pl")),
    new ApplicationFeature("Quu3", false, new Uri("https://www.wp.pl")),
    new ApplicationFeature("Qux2", true, new Uri("https://www.wp.pl")),
    new ApplicationFeature("Quu4", false, new Uri("https://www.wp.pl"))
};

var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();

var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

var featureManagerBuilder = new FeatureManagerBuilder(new FeatureSwitcherBasicClientConfiguration(
        applicationName: "Console",
        environmentName: "testEnv",
        environmentNodeAddress: new Uri("http://localhost:5216")))
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