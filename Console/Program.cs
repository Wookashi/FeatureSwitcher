﻿using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Implementation;
using Wookashi.FeatureSwitcher.Console;


var featureCollection = new List<IFeatureStateModel>
{
    new ApplicationFeature("Foo",true, new Uri("www.wp.pl")),
    new ApplicationFeature("Bar", false, new Uri("www.wp.pl")),
    new ApplicationFeature("Baz", true, new Uri("www.wp.pl")),
    new ApplicationFeature("Qux", false, new Uri("www.wp.pl")),
    new ApplicationFeature("Quu1", true, new Uri("www.wp.pl")),
    new ApplicationFeature("Quu3", false, new Uri("www.wp.pl")),
    new ApplicationFeature("Qux2", true, new Uri("www.wp.pl")),
    new ApplicationFeature("Quu4", false, new Uri("www.wp.pl")), //TODO should throw exc
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