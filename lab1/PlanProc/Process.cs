using System;
using System.Collections.Generic;
using System.IO;
namespace PlanProc
{
    public class Process
    {
        public string Name { get; set; }
        public string MatrixFile { get; set; }
        public string VectorFile { get; set; }
        public int ArrivalTime { get; set; }
        public int StartTime { get; set; }
        public int TerminationTime { get; set; }
        public int TotalWorkUnits { get; set; }
        public double UsedCpuTime { get; set; } 
        public double[] Solution { get; set; }
        public string ErrorMessage { get; set; }
        public int BurstTime { get; set; }
    }
}