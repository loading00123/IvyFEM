﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public class NewtonFluidMaterial : Material
    {
        public double MassDensity { get => Values[0]; set => Values[0] = value; }
        public double GravityX { get => Values[1]; set => Values[1] = value; }
        public double GravityY { get => Values[2]; set => Values[2] = value; }
        public double GravityZ { get => Values[3]; set => Values[3] = value; }
        public double Mu { get => Values[4]; set => Values[4] = value; }

        public NewtonFluidMaterial()
        {
            int len = 5;
            Values = new double[len];

            MassDensity = 1.0;
            GravityX = 0.0;
            GravityY = 0.0;
            GravityZ = 0.0;
            Mu = 1.0;
        }

        public NewtonFluidMaterial(NewtonFluidMaterial src) : base(src)
        {

        }

        public override void Copy(IObject src)
        {
            base.Copy(src);
        }
    }
}
