# WfWattch2Mqtt

Wattch2から取得したデータを MQTT 経由で受信し、InfluxDB v2へ保存するツールです。

## Requirements

- .NET
- MQTT Broker
- InfluxDB v2

## Configuration

### credentials.json

```json
{
  "broker": "YOUR_MQTT_BROKER",
  "port": 1883,
  "username": "username",
  "password": "password",
  "influxUrl": "http://YOUR_INFLUXDB_SERVER:8086",
  "influxToken": "YOUR_INFLUXDB_TOKEN",
  "influxBucket": "watt",
  "influxOrg": "YOUR_ORG"
}
```

### devices.txt

```json
[
  {
    "Name": "UPS01",
    "IpAddress": "192.168.10.1"
  },
  {
    "Name": "UPS02",
    "IpAddress": "192.168.10.2"
  }
]
```

## Features

- MQTTによるWattch2データ受信
- InfluxDB v2へのデータ格納
- 複数デバイス対応
- LinuxおよびWindows対応

## License

MIT License

## Acknowledgements

This project uses DeviceLib.WFWattch2.

Original project:
https://github.com/usausa/devicelib-wfwattch2
