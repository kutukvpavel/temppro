using System.IO;
using YamlDotNet.Serialization;

namespace TempProServer
{
    public enum TemperatureSensorTypes
    {
        Thermocouple = 1,
        RTD
    }

    [YamlSerializable]
    public class Configuration
    {
        [YamlIgnore]
        public static Configuration Instance { get; private set; } = new Configuration();
        public static void Load(string path)
        {
            var deserializer = new DeserializerBuilder().Build();
            using var reader = new StreamReader(path);
            Instance = deserializer.Deserialize<Configuration>(reader);
        }

        public Configuration() { }

        public TemperatureSensorTypes SensorType { get; set; } = TemperatureSensorTypes.Thermocouple; //Always TC for our accessory
        public int DeviceAddress { get; set; } = 1; //Always 1
        public double MaxRampRate { get; set; } = 100; // degC/min
        public double AssumedRoomTemperature { get; set; } = 30;
        public double MaxTemperature { get; set; } = 700;
        public double MinTemperature { get; set; } = 20;
        public double RampControlTolerance { get; set; } = 2.0;
        public int RampControlTimeout { get; set; } = 600; //Enforced if the temperature won't budge despite controllers efforts
    }
}