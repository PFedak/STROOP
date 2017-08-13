﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Structs
{
    public class FlyGuyDataTable
    {
        private static readonly int CycleSize = 64;

        public FlyGuyDataTable()
        {
            double[] relativeHeightOffsets = new double[CycleSize];
            double[] nextHeightDiffs = new double[CycleSize];

            for (int i = 0; i < CycleSize; i++)
            {
                double radians = (i / (double)CycleSize) * 2 * Math.PI;

            }
        }

        public double GetRelativeHeight(int oscillationTimer)
        {
            return -1;
        }

        public double GetNextHeightDiff(int oscillationTimer)
        {
            return 1;
        }

        public double GetMinHeight(int oscillationTimer)
        {
            return 2;
        }

        public double GetMaxHeight(int oscillationTimer)
        {
            return 3;
        }
    }
}
