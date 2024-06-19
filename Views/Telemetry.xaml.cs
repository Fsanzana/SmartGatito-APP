using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartGatito.Models;
namespace SmartGatito.Views;

public partial class Telemetry : ContentPage
{
    public Telemetry()
	{
        InitializeComponent();
        try
        {
            string json = @"[{""Time"":""11:00"",""EventName"":""Detección""},{""Time"":""11:30"",""EventName"":""Consumo""}]";
            List<Event> eventos = JsonConvert.DeserializeObject<List<Event>>(json); 
            miListView.ItemsSource = eventos;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        miListView.ItemSelected += MiListView_ItemSelected;

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