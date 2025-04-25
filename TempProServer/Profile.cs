using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace TempProServer
{
    public struct ProfilePoint
    {
        public ProfilePoint(int time, double temp)
        {
            Time = time;
            Temperature = temp;
        }

        public int Time { get; set; }
        public double Temperature { get; set; }
    }

    public enum SegmentTypes
    {
        Isothermal,
        CalculatedRamp,
        ControlledRamp
    }

    [YamlSerializable]
    public class ProfileSegment
    {
        public ProfileSegment() { }

        public double T { get; set; } //Target temp, degC
        public double? Ramp { get; set; } //Ramp rate, degC/min
        public bool? ControlRamp { get; set; } //True == wait until ramp target is actually reached
        public int? Total { get; set; } //Total duration, including theoretical ramp, seconds
        public int? Soak { get; set; } //Soak duration, wait until target temp is actually reached, seconds

        [YamlIgnore]
        public SegmentTypes Type {
            get {
                if (Ramp != null)
                {
                    if (ControlRamp ?? false)
                    {
                        return SegmentTypes.ControlledRamp;
                    }
                    return SegmentTypes.CalculatedRamp;
                }
                return SegmentTypes.Isothermal;
            }
        }
        [YamlIgnore]
        public int? StartTime { get; set; }
        [YamlIgnore]
        public int? ProjectedSoakStart { get; set; }
        [YamlIgnore]
        public int? ProjectedEnd { get; set; }

        public void Verify(Configuration cfg)
        {
            switch (Type)
            {
                case SegmentTypes.Isothermal:
                    ControlRamp = false;
                    if (Total == null)
                    {
                        if (Soak != null) Total = Soak;
                        else throw new InvalidDataException("No duration was specified for an isothermal segment");
                    }
                    if (Total <= 0) throw new InvalidDataException("Duration must be positive");
                    break;
                case SegmentTypes.CalculatedRamp:
                case SegmentTypes.ControlledRamp:
                    if (Ramp <= 0 || Ramp > cfg.MaxRampRate) throw new InvalidDataException("Ramp rate out of range");
                    if (Type == SegmentTypes.ControlledRamp && Total.HasValue)
                        throw new InvalidDataException("Controlled ramp can not be time-constrained with Total");
                    if ((Total ?? 1) <= 0) throw new InvalidDataException("Duration must be positive");
                    if ((Soak ?? 1) <= 0) throw new InvalidDataException("Duration must be positive");
                    break;
                default:
                    break;
            }
        }

        public int CalculateRampDuration(double initialTemp)
        {
            if (Type == SegmentTypes.Isothermal) throw new InvalidOperationException();
            return (int)Math.Ceiling(Math.Abs(initialTemp - T) / Ramp.Value * 60.0);
        }
    }

    [YamlSerializable]
    public class Profile
    {
        public static Profile Example { get; } = new Profile()
        {
            InitialWaitSeconds = 1,
            LimitRampRate = 100,
            AfterScriptT = 30,
            Segments = new ProfileSegment[] {
                new ProfileSegment() {
                    T = 30,
                    Ramp = null,
                    ControlRamp = false,
                    Total = 3600
                },
                new ProfileSegment() {
                    T = 100,
                    Ramp = 50,
                    ControlRamp = true,
                    Soak = 3600
                }
            }
        };
        public static Profile Load(string path)
        {
            var deserializer = new DeserializerBuilder().Build();
            using var reader = new StreamReader(path);
            return deserializer.Deserialize<Profile>(reader);
        }
        public static void Save(string path, Profile p)
        {
            var serializer = new SerializerBuilder().Build();
            using var writer = new StreamWriter(path);
            serializer.Serialize(writer, p);
        }

        public Profile() {
            Segments = Array.Empty<ProfileSegment>();
        }

        public int? InitialWaitSeconds { get; set; }
        public double? CommonRampRate { get; set; } //Enforce a single ramp rate for all segments (makes isothermals ramps!)
        public double? LimitRampRate { get; set; }
        public double? AfterScriptT { get; set; } //Temperature to set after script completion (no time constraints)
        public ProfileSegment[] Segments { get; set; }
        public bool EnableLog { get; set; } = true;

        [YamlIgnore]
        public int TotalTemperatures => Segments.Length + 1 + (AfterScriptT.HasValue ? 1 : 0);
        [YamlIgnore]
        public bool Verified { get; protected set; } = false;

        public List<ProfilePoint> VerifyAndCalculatePlot(Configuration cfg, TextWriter feedback)
        {
            List<ProfilePoint> plot = new(TotalTemperatures)
            {
                new ProfilePoint(0, cfg.AssumedRoomTemperature)
            };
            int l = InitialWaitSeconds ?? 0;
            if (l < 0) throw new InvalidDataException("Initial wait time must be non-negative");
            double temp = cfg.AssumedRoomTemperature;
            if (temp < cfg.MinTemperature || temp > cfg.MaxTemperature) throw new InvalidDataException("Assumed RT out of range");
            if (LimitRampRate > cfg.MaxRampRate || LimitRampRate <= 0) throw new InvalidDataException("Ramp limit out of range");
            for (int i = 0; i < Segments.Length; i++)
            {
                var segment = Segments[i];
                if (CommonRampRate.HasValue && !segment.Ramp.HasValue)
                {
                    segment.Ramp = CommonRampRate;
                }
                if (segment.Ramp > LimitRampRate) throw new InvalidDataException("Specified ramp rate is out of range");
                try
                {
                    segment.Verify(cfg);
                }
                catch (InvalidDataException ex)
                {
                    throw new InvalidDataException($"Invalid segment #{i}: {ex.Message}");
                }
                switch (segment.Type)
                {
                    case SegmentTypes.Isothermal:
                        break;
                    case SegmentTypes.CalculatedRamp:
                        {
                            int rampDuration = segment.CalculateRampDuration(temp);
                            if (segment.Total.HasValue)
                            {
                                if (rampDuration > segment.Total) throw new InvalidDataException($"Insufficient time for ramp for segment #{i}");
                            }
                            else
                            {
                                segment.Total = rampDuration + (segment.Soak ?? 0);
                            }
                            break;
                        }
                    case SegmentTypes.ControlledRamp:
                        {
                            segment.Total = segment.CalculateRampDuration(temp) + (segment.Soak ?? 0);
                            break;
                        }
                    default:
                        throw new InvalidOperationException();
                }
                l += segment.Total.Value;
                temp = segment.T;
                if (temp < cfg.MinTemperature || temp > cfg.MaxTemperature) throw new InvalidDataException($"T is out of range for segment #{i}");
                plot.Add(new ProfilePoint(l, temp));
                feedback.WriteLine(@$"Parsed segment of type = {Enum.GetName(typeof(SegmentTypes), segment.Type)}, T at the end = {temp}, segment len = {segment.Total}, total len = {l}");
            }
            if (AfterScriptT.HasValue)
            {
                plot.Add(new ProfilePoint(l, AfterScriptT.Value));
            }
            Verified = true;
            return plot;
        }
    }
}