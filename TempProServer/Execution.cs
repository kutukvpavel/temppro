using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.IO;

namespace TempProServer
{
    public enum ExecutionStates
    {
        Idle,
        Running,
        Paused,
        Error
    }

    public class Execution
    {
        protected enum EngineStates
        {
            Idle,
            WaitingIsothermal,
            CalculatedRamping,
            ControlledRamping,
            Soaking,
            SetupIsothermal,
            SetupCalculatedRamp,
            SetupControlledRamp,
            End
        }

        protected readonly Profile _Profile;
        protected readonly System.Timers.Timer _Timer;
        protected readonly Configuration _Config;
        protected readonly Controller _Controller;
        protected EngineStates _EngineState = EngineStates.Idle;
        protected Semaphore _ForceNextSemaphore = new Semaphore(0, 1);
        protected Queue<double> _TempTrend;

        protected void SetSetpoint(double t)
        {
            _Controller.SetSetpoint(t);
            CurrentSetpoint = t;
        }
        protected void ExecutionEngine(int localTime)
        {
            CurrentTemperature = _Controller.GetTemperature();
            if (_TempTrend.Count >= _Config.RampControlTimeout) _TempTrend.Dequeue();
            _TempTrend.Enqueue(CurrentTemperature);
            ProfileSegment? currentSegment = (SegmentIndex >= 0) ? _Profile.Segments[SegmentIndex] : null;
            bool advance = _ForceNextSemaphore.WaitOne(0);
            switch (_EngineState)
            {
                case EngineStates.Idle:
                    if (localTime >= (_Profile.InitialWaitSeconds ?? 0)) advance = true;
                    break;
                case EngineStates.Soaking:
                case EngineStates.WaitingIsothermal:
                    if (localTime >= currentSegment.ProjectedEnd.Value) advance = true;
                    break;
                case EngineStates.CalculatedRamping:
                    if (localTime >= currentSegment.ProjectedSoakStart.Value) _EngineState = EngineStates.Soaking;
                    break;
                case EngineStates.ControlledRamping:
                    if ((Math.Abs(CurrentTemperature - currentSegment.T) < _Config.RampControlTolerance) ||
                        ((localTime - currentSegment.StartTime) > _Config.RampControlTimeout) &&
                        ((_TempTrend.Max() - _TempTrend.Min()) < _Config.RampControlTolerance))
                    {
                        currentSegment.ProjectedEnd = localTime + currentSegment.Soak.Value;
                        _EngineState = EngineStates.Soaking;
                    }
                    break;
                default:
                    break;
            }
            if (advance)
            {
                ProfileSegment? nextSegment = ((SegmentIndex + 1) < _Profile.Segments.Length) ?
                    _Profile.Segments[SegmentIndex + 1] : null;
                if (nextSegment == null)
                {
                    _EngineState = EngineStates.End;
                }
                else
                {
                    nextSegment.StartTime = localTime;
                    nextSegment.ProjectedEnd = nextSegment.StartTime + nextSegment.Total.Value;
                    nextSegment.ProjectedSoakStart = nextSegment.ProjectedEnd - (nextSegment.Soak ?? 0);
                    SegmentIndex++;
                    _EngineState = nextSegment.Type switch
                    {
                        SegmentTypes.Isothermal => EngineStates.SetupIsothermal,
                        SegmentTypes.CalculatedRamp => EngineStates.SetupCalculatedRamp,
                        SegmentTypes.ControlledRamp => EngineStates.SetupControlledRamp,
                        _ => throw new InvalidOperationException(),
                    };
                }
                switch (_EngineState)
                {
                    case EngineStates.SetupIsothermal:
                        CanOverride = true;
                        _Controller.SetRampControl(false);
                        SetSetpoint(nextSegment.T);
                        _EngineState = EngineStates.WaitingIsothermal;
                        break;
                    case EngineStates.SetupCalculatedRamp:
                    case EngineStates.SetupControlledRamp:
                        CanOverride = false;
                        _Controller.SetRampControl(true);
                        _Controller.SetRampRate(nextSegment.Ramp.Value);
                        SetSetpoint(nextSegment.T);
                        _EngineState = _EngineState switch
                        {
                            EngineStates.SetupCalculatedRamp => EngineStates.CalculatedRamping,
                            EngineStates.SetupControlledRamp => EngineStates.ControlledRamping,
                            _ => throw new InvalidOperationException(),
                        };
                        break;
                    case EngineStates.End:
                        if (_Profile.AfterScriptT.HasValue)
                        {
                            _Controller.SetRampControl(false);
                            SetSetpoint(_Profile.AfterScriptT.Value);
                        }
                        Abort();
                        break;
                    default:
                        break;
                }
            }
        }

        protected void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                ExecutionEngine(++TimePast);
            }
            catch (Exception primaryEx)
            {
                Abort();
                State = ExecutionStates.Error;
                try
                {
                    _Controller.SetRampControl(false);
                    SetSetpoint(_Config.AssumedRoomTemperature);
                }
                catch (Exception secondaryEx)
                {
                    ExceptionOccurred?.Invoke(this, secondaryEx);
                }
                ExceptionOccurred?.Invoke(this, primaryEx);
            }
            float progress = TimeEstimationIsAccurate ? (float)TimePast / EstimatedLength : (float)SegmentIndex / _Profile.Segments.Length;
            if (progress < 0) progress = 0;
            Progress.Report(progress);
            LogEvent?.Invoke(this, CurrentTemperature);
        }

        public event EventHandler<Exception>? ExceptionOccurred;
        public event EventHandler<double>? LogEvent;

        public Execution(Profile prf, Controller ctrl, Configuration cfg)
        {
            _Profile = prf;
            _Controller = ctrl;
            _Timer = new(1000) { AutoReset = true, Enabled = false };
            _Timer.Elapsed += Timer_Elapsed;
            _Config = cfg;
            Progress = new Progress<float>();
            CurrentSetpoint = _Config.AssumedRoomTemperature;
            _TempTrend = new Queue<double>(_Config.RampControlTimeout);
        }

        public IProgress<float> Progress { get; }
        public int SegmentIndex { get; protected set; } = -1;
        public List<ProfilePoint> Simulation { get; protected set; } = new List<ProfilePoint>();
        public bool TimeEstimationIsAccurate { get; protected set; } = true;
        public int EstimatedLength => Simulation.LastOrDefault().Time;
        public ExecutionStates State { get; protected set; } = ExecutionStates.Idle;
        public int TimePast { get; protected set; } = 0;
        public int TimeRemaining {
            get {
                int r = EstimatedLength - TimePast;
                return r >= 0 ? r : -r;
            }
        }
        public double CurrentSetpoint { get; protected set; }
        public double CurrentTemperature { get; protected set; } = double.NaN;
        public bool CanOverride { get; protected set; } = false;

        public void Start()
        {
            if (!_Profile.Verified) throw new InvalidOperationException("Profile has to be verified before execution");
            if (!_Controller.IsInitialized) throw new InvalidOperationException("Controller has to be initialized before execution");
            State = ExecutionStates.Running;
            CanOverride = true;
            if (_Profile.LimitRampRate.HasValue)
            {
                _Controller.SetRampRate(_Profile.LimitRampRate.Value);
                _Controller.SetRampControl(true);
            }
            _Timer.Start();
        }
        public void Pause()
        {
            _Timer.Stop();
            State = ExecutionStates.Paused;
        }
        public void Abort()
        {
            if (State == ExecutionStates.Idle) return;
            _Timer.Stop();
            SegmentIndex = -1;
            TimePast = 0;
            Progress.Report(0);
            State = ExecutionStates.Idle;
            _EngineState = EngineStates.Idle;
        }
        public void ForceNext()
        {
            if (State != ExecutionStates.Running) return;
            try
            {
                _ForceNextSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                
            }
        }
        public void OverrideIsothermalSegment(double v)
        {
            if (State != ExecutionStates.Running || !CanOverride) return;
            SetSetpoint(v);
        }
        public Exception? VerifyAndCalculate(TextWriter feedback)
        {
            try
            {
                Simulation = _Profile.VerifyAndCalculatePlot(_Config, feedback);
                TimeEstimationIsAccurate = !_Profile.Segments.Any(x => x.Type == SegmentTypes.ControlledRamp);
                feedback.WriteLine($"Time estimation is accurate: {TimeEstimationIsAccurate}");
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}