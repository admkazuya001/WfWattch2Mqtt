#nullable enable

using DeviceLib.WFWattch2;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // 設定ファイルの読み込み
        var devices = JsonSerializer.Deserialize<List<DeviceInfo>>(System.IO.File.ReadAllText("devices.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var creds = JsonSerializer.Deserialize<CredentialInfo>(System.IO.File.ReadAllText("credentials.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        // MQTTの準備
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttOptions = new MqttClientOptionsBuilder().WithTcpServer(creds.Broker, creds.Port).WithCredentials(creds.Username, creds.Password).Build();

        // InfluxDB v2 クライアント
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", creds.InfluxToken);
   
        // キャンセル処理の準備
        using var appCts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; appCts.Cancel(); };

        Console.WriteLine("Wattch2Mqtt Service (Direct Auth Mode) Started.");

        // メインループ
        while (!appCts.IsCancellationRequested)
        {
            foreach (var device in devices)
            {
                try
                {
                    var ip = System.Net.IPAddress.Parse(device.Ip);
                    //using var client = new DeviceLib.WFWattch2.WattchClient(ip);

                    var client = new MyWattchClient(device.Ip);

                    if (await client.UpdateAsync(appCts.Token))
                    {
                        Console.WriteLine($"[{device.Name}] {client.Power:F1}W / {client.Voltage:F1}V");

                        // --- 1. InfluxDB v2 直接送信 ---
                        try
                        {
                            var lineProtocol =
                                $"power,device={device.Name} " +
                                $"watt={client.Power.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                $"voltage={client.Voltage.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                            var influxUrl =
                                $"{creds.InfluxUrl.TrimEnd('/')}/api/v2/write" +
                                $"?org={Uri.EscapeDataString(creds.InfluxOrg)}" +
                                $"&bucket={Uri.EscapeDataString(creds.InfluxBucket)}" +
                                $"&precision=ns";

                            using var content = new StringContent(
                                lineProtocol,
                                Encoding.UTF8,
                                "text/plain");

                            using var response =
                                await httpClient.PostAsync(influxUrl, content);

                            Console.WriteLine(influxUrl);
                            Console.WriteLine(lineProtocol);
                            Console.WriteLine($"Org=[{creds.InfluxOrg}]");
                            Console.WriteLine($"Bucket=[{creds.InfluxBucket}]");
                            Console.WriteLine($"Url=[{creds.InfluxUrl}]");

                            if (response.IsSuccessStatusCode)
                            {
                                Console.WriteLine("[InfluxDB] Write OK");
                            }
                            else
                            {
                                var errorBody = await response.Content.ReadAsStringAsync();

                                Console.WriteLine(
                                    $"[InfluxDB Error] " +
                                    $"{(int)response.StatusCode} {response.StatusCode}: {errorBody}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[InfluxDB Exception] {ex}");
                        }

                        // --- 2. MQTT 送信 ---
                        try
                        {
                            if (!mqttClient.IsConnected) await mqttClient.ConnectAsync(mqttOptions, appCts.Token);
                            var payload = JsonSerializer.Serialize(new { name = device.Name, power = client.Power, voltage = client.Voltage });
                            var msg = new MqttApplicationMessageBuilder()
                                .WithTopic($"sensors/{device.Name}/telemetry")
                                .WithPayload(payload)
                                .Build();
                            await mqttClient.PublishAsync(msg, appCts.Token);
                        }
                        catch { /* MQTT失敗はスルー */ }
                    }
                    else
                    {
                        Console.WriteLine($"[{device.Name}] Failed to fetch data.");
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[{device.Name}] Error: {ex.Message}"); }
            }

            // 次の計測まで待機
            await Task.Delay(5000, appCts.Token);
        }
    } // Main の閉じ

    public class MyWattchClient
    {
        private readonly IPEndPoint _endPoint;
        public double Power { get; private set; }
        public double Voltage { get; private set; }

        public MyWattchClient(string ip) => _endPoint = new IPEndPoint(IPAddress.Parse(ip), 60121); // ポート 60121 固定

        public async Task<bool> UpdateAsync(CancellationToken ct)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3)); // 3秒で諦める

                await socket.ConnectAsync(_endPoint, timeoutCts.Token);

                // データ要求コマンド (Qiitaで実績のあるパケット)
                var cmd = new byte[] { 0xAA, 0x00, 0x02, 0x18, 0x00, 0x65 };
                await socket.SendAsync(cmd.AsMemory(), SocketFlags.None, ct);

                var buffer = new byte[64];
                var read = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, ct);

                if (read >= 23 && buffer[0] == 0xAA)
                {
                    // 解析ロジック (リトルエンディアン)
                    static long ReadValue(ReadOnlySpan<byte> b) =>
                        ((long)b[5] << 40) + ((long)b[4] << 32) + ((long)b[3] << 24) + ((long)b[2] << 16) + ((long)b[1] << 8) + b[0];

                    Voltage = (double)ReadValue(buffer.AsSpan(5, 6)) / (1L << 24);
                    Power = (double)ReadValue(buffer.AsSpan(17, 6)) / (1L << 24);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }

    // --- データモデル (Program クラスのメンバとして定義) ---
    public class DeviceInfo
    {
        [JsonPropertyName("Name")] public required string Name { get; init; }
        [JsonPropertyName("Ip")] public required string Ip { get; init; }
    }



}
// Program のpublic class CredentialInfo
   public class CredentialInfo
    {
    [JsonPropertyName("Broker")]
    public required string Broker { get; init; }

    [JsonPropertyName("Port")]
    public required int Port { get; init; }

    [JsonPropertyName("Username")]
    public required string Username { get; init; }

    [JsonPropertyName("Password")]
    public required string Password { get; init; }

    [JsonPropertyName("InfluxUrl")]
    public required string InfluxUrl { get; init; }

    [JsonPropertyName("InfluxToken")]
    public required string InfluxToken { get; init; }

    [JsonPropertyName("InfluxBucket")]
    public required string InfluxBucket { get; init; }

    [JsonPropertyName("InfluxOrg")]
    public required string InfluxOrg { get; init; }
   }