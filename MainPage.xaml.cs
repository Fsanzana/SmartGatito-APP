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
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Extensions.Rpc;
using MQTTnet;
using MQTTnet.Internal;

namespace SmartGatito
{
    public partial class MainPage : ContentPage 
    {
        string status = "Modo: Automático";
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
            var mqttListener = new Thread(getMqttMessage);
            mqttListener.Start();
            mqttListener.Join();
            var loadVideoStream = new Thread(LoadVideoStream);
            loadVideoStream.Start();
            loadVideoStream.Join();
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
                Console.WriteLine(e.Message);   
            }  


        }

        public async void getMqttMessage()
        {
            var subscribed = false;
            // Create a new MQTT client.
            var mqttFactory = new MqttFactory();
            var mqttClient = mqttFactory.CreateManagedMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                   .WithClientId(mqttClientId)
                   .WithTcpServer(mqttServer, mqttPort)
                   .WithCredentials(mqttUsername, mqttPassword)
                   .Build();
            var connectResult = new MqttClientConnectResult();
            try
            {
                await mqttClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(mqttClientOptions)
                    .Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
           
            Console.WriteLine(connectResult.ResultCode);
            try
            {
                if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    // Wait until the queue is fully processed.
                    Console.WriteLine("Connected to MQTT broker successfully.");
                    SpinWait.SpinUntil(() => mqttClient.PendingApplicationMessagesCount == 0, 10000);

                    Console.WriteLine($"Pending messages = {mqttClient.PendingApplicationMessagesCount}");
                    // Subscribe to a topic
                    await mqttClient.SubscribeAsync("v1/devices/me/attributes");

                    SpinWait.SpinUntil(() => subscribed, 1000);
                    Console.WriteLine("Subscription properly done");
                    // Callback function when a message is received
                    mqttClient.ApplicationMessageReceivedAsync += e =>
                    {
                        Console.WriteLine(e.AutoAcknowledge);
                        var message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                        Console.WriteLine("Received message:");
                        Console.WriteLine(message);
                        if (message.Contains("agua") || message.Contains("detectado"))
                        {
                            var key = "";
                            int value = 0;
                            if(message.Contains("detectado"))
                            {
                                key = "detectado";
                                message = message.Replace("{\"detectado\":", "");
                                message = message.Replace("}", "");
                            }
                            else
                            {
                                key = "agua";
                                message = message.Replace("{\"agua\":", "");
                                message = message.Replace("}", "");
                            }
                            value = value + Int32.Parse(message);
                            string veceslabel = "";
                            string countLabel = "";
                            if (value != 1)
                            {
                                veceslabel = "veces";
                            }
                            else
                            {
                                veceslabel = "vez";
                            }

                            if (value < 10)
                            {
                                countLabel = "0" + value.ToString();
                            }
                            else
                            {
                                countLabel = value.ToString();
                            }
                            if (key == "agua")
                            {
                                Action updateLabels = () =>
                                {
                                    waterVeces.Text = veceslabel;
                                    waterCountLabel.Text = countLabel;
                                    SemanticScreenReader.Announce(waterCountLabel.Text);
                                    SemanticScreenReader.Announce(waterVeces.Text);
                                };
                                Dispatcher.Dispatch(updateLabels);
                            }else if(key == "detectado")
                            {
                                Action updateLabels = () =>
                                {
                                    detectVeces.Text = veceslabel;
                                    detectCountLabel.Text = countLabel;
                                    SemanticScreenReader.Announce(detectCountLabel.Text);
                                    SemanticScreenReader.Announce(detectVeces.Text);
                                };
                                Dispatcher.Dispatch(updateLabels);
                            }
                        }
                        
                        return Task.CompletedTask;
                    };




                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
                Console.WriteLine(e.Message);
            }


            
        }


        private void LoadVideoStream()
        {
            var videoStreamUrl = "http://192.168.1.126:5000/video";
            Action getStream = () =>
            {
                videoWebView.Source = new HtmlWebViewSource
                {
                    Html = $@"
                <html>
                <body style='margin:0;padding:0;'>
                    <img src='{videoStreamUrl}' style='width:100%;height:auto;' />
                </body>
                </html>"
                };
            };
            Dispatcher.Dispatch(getStream);
        }
        
        


    }  
}
