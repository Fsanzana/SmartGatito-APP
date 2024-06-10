using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmartGatito.Helpers;
using Thingsboard.Net.Flurl.Options;
using Thingsboard.Net.Flurl;
using System.Net;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Extensions.Rpc;
using MQTTnet;

namespace SmartGatito
{
    public partial class MainPage : ContentPage
    {
        string status = "Modo: Automático";
        string waterData = "";
        int waterCount = 0;
        string token = "";
        string username = "";
        string password = "";
        string deviceId = "";
        string mqttServer = "";
        int mqttPort = 0;
        string mqttClientId = "";
        string mqttUsername = "";
        string mqttPassword = "";
        private const string Namespace = "SmartGatito";
        private const string FileName = "secrets.json";
        public MainPage()
        {  

            InitializeComponent();
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{Namespace}.{FileName}");
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            username = config.GetSection("API:username").Value;
            password = config.GetSection("API:password").Value;
            deviceId = config.GetSection("API:deviceId").Value;
            mqttServer = config.GetSection("broker:host").Value;
            mqttPort = Int32.Parse(config.GetSection("broker:port").Value);
            mqttClientId = config.GetSection("broker:clientId").Value;
            mqttUsername = config.GetSection("broker:username").Value;
            mqttPassword = config.GetSection("broker:password").Value;
            
        }
        protected async override void OnAppearing()
        {
            await getJwtToken();
            await setMode(2);
            Thread mqttListener = new Thread(new ThreadStart(getWaterCount));
            mqttListener.Start();
            mqttListener.Join();            
        }

      




        private async void OnMode(object sender, EventArgs e)
        {
            modeOn.BackgroundColor = Color.FromHex("#20EE20");
            modeOn.Opacity = 0.6;
            modeAuto.BackgroundColor = Color.FromHex("#AAAAAA");
            modeAuto.Opacity = 0.6;
            modeOff.BackgroundColor = Color.FromHex("#AAAAAA");
            modeOff.Opacity = 0.6;
            await setMode(1);
        }
        private async void AutoMode(object sender, EventArgs e)
        {
            modeAuto.BackgroundColor = Color.FromHex("#20EE20");
            modeAuto.Opacity = 0.6;
            modeOn.BackgroundColor = Color.FromHex("#AAAAAA");
            modeOn.Opacity = 0.6;
            modeOff.BackgroundColor = Color.FromHex("#AAAAAA");
            modeOff.Opacity = 0.6;
            await setMode(2);
        }
        private async void OffMode(object sender, EventArgs e)
        {
            modeOff.BackgroundColor = Color.FromHex("#20EE20");
            modeOff.Opacity = 0.6;
            modeAuto.BackgroundColor = Color.FromHex("#AAAAAA");
            modeAuto.Opacity = 0.6;
            modeOn.BackgroundColor = Color.FromHex("#AAAAAA");
            modeOn.Opacity = 0.6;
            await setMode(3);
        }

        public async Task setMode(int mode)
        {        
            HttpClient client = new HttpClient();
            var uri = new Uri("http://iot.ceisufro.cl:8080/api/plugins/telemetry/DEVICE/"+ deviceId+ "/SHARED_SCOPE");
            client.DefaultRequestHeaders.Add("X-Authorization", "Bearer " + token);
            var content = new StringContent("{\"modo\":\"" + mode + "\"}", Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await client.PostAsync(uri, content);
                string responseString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                waterData = e.ToString();
            }  


        }

        public async void getWaterCount()
        {         
            // Create a new MQTT client.
            var mqttFactory = new MqttFactory();
            var mqttClient = mqttFactory.CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                   .WithClientId(mqttClientId)
                   .WithTcpServer(mqttServer, mqttPort)
                   .WithCredentials(mqttUsername, mqttPassword)
                   .Build();
            
            var connectResult = await mqttClient.ConnectAsync(mqttClientOptions);
            Console.WriteLine(connectResult.ResultCode);
            waterData = connectResult.ResultCode.ToString();
            if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                
                Console.WriteLine("Connected to MQTT broker successfully.");

                // Subscribe to a topic
                await mqttClient.SubscribeAsync("v1/devices/me/attributes");


                // Callback function when a message is received
                mqttClient.ApplicationMessageReceivedAsync += e =>
                {               
                    var message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    Console.WriteLine(message);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        waterData = message;
                        Console.WriteLine($"{waterCountLabel.Text}");
                        if (waterData.Contains("agua")){
                            waterData = waterData.Replace("{\"agua\":", "");
                            waterData = waterData.Replace("}", "");
                            waterCount = waterCount+Int32.Parse(waterData);
                            
                            SemanticScreenReader.Announce(waterCountLabel.Text);
                            if(waterCount != 1)
                            {
                                waterVeces.Text = "veces";
                                SemanticScreenReader.Announce(waterVeces.Text);
                            }
                            else
                            {
                                waterVeces.Text = "vez";
                                SemanticScreenReader.Announce(waterVeces.Text);
                            }
                            if(waterCount < 10)
                            {                    
                                waterCountLabel.Text = "0"+ waterCount.ToString();
                                SemanticScreenReader.Announce(waterCountLabel.Text);
                            }
                            else
                            {
                                waterCountLabel.Text = waterCount.ToString();
                            }
                        }
                    });
                    return Task.CompletedTask;
                };




            }
        }
        public async Task getJwtToken()

        {   
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Host", "iot.ceisufro.cl:8080"); 
            var uri = new Uri("http://iot.ceisufro.cl:8080/api/auth/login");
            var content = new StringContent("{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}", Encoding.UTF8, "application/json");
            HttpResponseMessage response = new HttpResponseMessage();
           

            try
            {
                response = await client.PostAsync("http://iot.ceisufro.cl:8080/api/auth/login", content);
                string responseString = await response.Content.ReadAsStringAsync();
                var jwt = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
                token = jwt["token"];
            }
            catch(Exception e)
            {
                waterData = e.Message;
            }
            
        }





    }  
}
