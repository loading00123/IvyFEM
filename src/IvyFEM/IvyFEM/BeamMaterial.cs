﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public class BeamMaterial : Material
    {
        public double Area { get => Values[0]; set => Values[0] = value; }
        public double SecondMomentOfArea { get => Values[1]; set => Values[1] = value; }
        public double MassDensity { get => Values[2]; set => Values[2] = value; }
        public double Young { get => Values[3]; set => Values[3] = value; }

        public BeamMaterial()
        {
            int len = 4;
            Values = new double[len];

            Area = 0.0;
            SecondMomentOfArea = 0.0;
            MassDensity = 0.0;
            Young = 0.0;
        }

        public BeamMaterial(BeamMaterial src) : base(src)
        {

        }

        public override void Copy(IObject src)
        {
            base.Copy(src);
        }

    }
}
