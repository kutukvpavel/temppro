using System;
using System.IO;

namespace TempProServer
{
    /// <summary>
    /// This class wraps around the ugly decompiled code that handles low-level stuff
    /// </summary>
    public class Controller : ControllerBase
    {
        protected readonly Configuration Config;
        protected readonly Exception NotInitializedExcepton = new InvalidOperationException("Not initialized");

        public Controller(Configuration cfg) : base()
        {
            Config = cfg;
        }

        public object LockObject { get; } = new();
        public bool IsInitialized { get; protected set; } = false;
        public bool IsError => !commok;

        public void Init()
        {
            bDegF = false;
            lock (LockObject)
            {
                if (OpenUSBPort())
                {
                    int num = QueryController(this, Config.DeviceAddress);
                    num = QueryController(this, Config.DeviceAddress);
                    if (num > 0 && num < 1000)
                    {
                        EZ_Zone = true;
                        SetCommstoTempUnitsC(this, Config.DeviceAddress);
                    }
                    else
                    {
                        EZ_Zone = false;
                    }
                }
                if (OpenUSBPort() && (SetTempUnitsC(this, Config.DeviceAddress) != 0f))
                {
                    if (!EZ_Zone)
                    {
                        SetCommstoTempUnitsF(this, Config.DeviceAddress);
                        bDegF = true;
                    }
                    Console.WriteLine("USB Port opened OK");
                    Console.WriteLine($"Current Temp = {GetProcessValue(this, Config.DeviceAddress, bDegF)}");
                    sngSetPointHighLimit = Config.SensorType switch
                    {
                        TemperatureSensorTypes.Thermocouple => GetTCSetpointHighLimit(this, Config.DeviceAddress, bDegF),
                        TemperatureSensorTypes.RTD => GetRTDSetpointHighLimit(this, Config.DeviceAddress, bDegF),
                        _ => throw new ArgumentException(),
                    };
                }
                else
                {
                    Console.WriteLine("Failed to connect to controller");
                    throw new InvalidDataException();
                }
                IsInitialized = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v">degC</param>
        public void SetSetpoint(double v)
        {
            if (!IsInitialized) throw NotInitializedExcepton;
            lock (LockObject)
            {
                SetSetPoint(this, Config.DeviceAddress, v, bDegF);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v">degC/min</param>
        public void SetRampRate(double v)
        {
            if (!IsInitialized) throw NotInitializedExcepton;
            lock (LockObject)
            {
                SetRampRateDegPerMin(this, Config.DeviceAddress);
                SetRampRate(this, Config.DeviceAddress, v);
            }
        }

        public void SetRampControl(bool enable)
        {
            if (!IsInitialized) throw NotInitializedExcepton;
            if (enable)
            {
                lock (LockObject)
                {
                    SetEnableRampControl(this, Config.DeviceAddress);
                }
            }
            else
            {
                lock (LockObject)
                {
                    SetDisableRampControl(this, Config.DeviceAddress);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>degC</returns>
        public double GetTemperature()
        {
            if (!IsInitialized) throw NotInitializedExcepton;
            double ret;
            lock (LockObject)
            {
                ret = GetProcessValue(this, Config.DeviceAddress, bDegF);
            }
            return ret;
        }
    }
}