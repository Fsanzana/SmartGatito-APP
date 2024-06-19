using System.Collections.Generic;
using System.Reflection;
using Microcharts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkiaSharp;
using SmartGatito.Models;
namespace SmartGatito.Views;

public partial class Telemetry : ContentPage
{
    private const string Namespace = "SmartGatito.Data";
    private const string FileName = "telemetryTestData.json";
    ChartEntry[] entries = new[]
    {
        new ChartEntry(1.5f)
        {
            Label = "Lun",
            ValueLabel = "1.5L",
            Color = SKColor.Parse("#00b4d8"),
            TextColor = SKColor.Parse("#ffffff"),
            OtherColor = SKColor.Parse("#ffffff")
        },
        new ChartEntry(1.2f)
        {
            Label = "Mar",
            ValueLabel = "1.2L",
            Color = SKColor.Parse("#00b4d8"),
            TextColor = SKColor.Parse("#ffffff"),
            OtherColor = SKColor.Parse("#ffffff")
        },
        new ChartEntry(2.1f)
        {
            Label = "Mier",
            ValueLabel = "2.1L",
            Color = SKColor.Parse("#00b4d8"),
            TextColor = SKColor.Parse("#ffffff"),
            OtherColor = SKColor.Parse("#ffffff")
        },
        new ChartEntry(2)
        {
            Label = "Jue",
            ValueLabel = "2L",
            Color = SKColor.Parse("#00b4d8"),
            TextColor = SKColor.Parse("#ffffff"),
            OtherColor = SKColor.Parse("#ffffff")
        },
        new ChartEntry(1.9f)
        {
            Label = "Vie",
            ValueLabel = "1.9L",
            Color = SKColor.Parse("#00b4d8"),
            TextColor = SKColor.Parse("#ffffff"),
            OtherColor = SKColor.Parse("#ffffff")
        }
    };
    public Telemetry()
	{
        InitializeComponent();
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{Namespace}.{FileName}");
            StreamReader reader = new StreamReader(stream);
            string json = reader.ReadToEnd(); //Make string equal to full file
            
            List<Event> eventos = JsonConvert.DeserializeObject<List<Event>>(json); 
            miListView.ItemsSource = eventos;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        miListView.ItemSelected += MiListView_ItemSelected;
        try
        {
            chartView.Chart = new LineChart
            {
                Entries = entries,
                BackgroundColor = SKColor.Parse("#f8f6f0"),
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                            
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }
    private void MiListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem != null)
        {
            string seleccionado = e.SelectedItem as string;
            // Haz algo con el elemento seleccionado
            DisplayAlert("Elemento Seleccionado", seleccionado, "OK");

            // Deseleccionar elemento
            ((ListView)sender).SelectedItem = null;
        }
    }
}