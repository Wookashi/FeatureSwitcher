using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Wookashi.FeatureSwitcher.Client.Implementation;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;


var featureCollection = new List<FeatureStateModel>
{
    new("Foo",true),
    new("Bar", false),
    new("Baz", true),
    new("Qux", false),
    new("Quu1", true),
    new("Quu3", false),
    new("Qux2", true),
    new("Quu3", false), //should throw exception
};

var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();

var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

var featureManager = new FeatureManager(httpClientFactory!, "Console", "testEnv", featureCollection);
await featureManager.RegisterFeaturesOnNode();

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