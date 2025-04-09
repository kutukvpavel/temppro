using System;
using System.Collections.Generic;
using TempProServer;

namespace TempProGui.Models
{
    public class Model
    {
        protected List<ProfilePoint> _EmptyPlot = new();

        public Model(Configuration cfg)
        {
            Config = cfg;
            Controller = new(cfg);
        }

        public Configuration Config { get; protected set; }
        public Controller Controller { get; }
        public Execution? ProfileExecution { get; protected set; } = null;
        public List<ProfilePoint> PlotData => ProfileExecution?.Simulation ?? _EmptyPlot;

        public Exception? SetupProfileExecution(string path)
        {
            ProfileExecution = new Execution(Profile.Load(path), Controller, Config);
            return ProfileExecution.VerifyAndCalculate();
        }


    }
}