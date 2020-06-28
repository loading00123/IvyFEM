﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public class TriangleFEConstantInterpolate : IInterpolate
    {
        public TriangleFE Owner { get; set; }

        public TriangleFEConstantInterpolate()
        {

        }

        public TriangleFEConstantInterpolate(TriangleFE owner)
        {
            Owner = owner;
        }


        public uint GetNodeCount()
        {
            return 1;
        }

        public double[] GetNodeL(int nodeId)
        {
            double[][] nodeL = new double[1][]
            {
                new double[] { 1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0 }
            };
            return nodeL[nodeId];
        }

        public double[] CalcN(double[] L)
        {
            double[] N = new double[1];

            // N
            N[0] = 1.0;

            return N;
        }

        public double[][] CalcNu(double[] L)
        {
            double[][] Nu = new double[1][];

            // dN/dx
            Nu[0] = new double[1] { 0.0 };

            // dN/dy
            Nu[1] = new double[1] { 0.0 };
            return Nu;
        }

        public double[,][] CalcNuv(double[] L)
        {
            double[,][] Nuv = new double[2, 2][];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    double[] value = new double[3];
                    Nuv[i, j] = value;
                    value[0] = 0;
                    value[1] = 0;
                    value[2] = 0;
                }
            }
            return Nuv;
        }

        public double[] CalcSN()
        {
            double A = Owner.GetArea();
            double[] sN = new double[1] { A };
            return sN;
        }

        public double[,] CalcSNN()
        {
            double A = Owner.GetArea();
            double[,] sNN = new double[1, 1]
            {
                { A }
            };
            return sNN;
        }

        public double[,][,] CalcSNuNv()
        {
            double A = Owner.GetArea();
            double[] a;
            double[] b;
            double[] c;
            Owner.CalcTransMatrix(out a, out b, out c);

            double[,][,] sNuNv = new double[2, 2][,];

            // sNxNx
            sNuNv[0, 0] = new double[1, 1];
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    sNuNv[0, 0][i, j] = 0.0;
                }
            }

            // sNxNy
            sNuNv[0, 1] = new double[1, 1];
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    sNuNv[0, 1][i, j] = 0.0;
                }
            }

            // sNyNx
            sNuNv[1, 0] = new double[1, 1];
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    sNuNv[1, 0][i, j] = 0.0;
                }
            }

            // sNyNy
            sNuNv[1, 1] = new double[1, 1];
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    sNuNv[1, 1][i, j] = 0.0;
                }
            }
            return sNuNv;
        }

        public double[][,] CalcSNuN()
        {
            double A = Owner.GetArea();
            double[] a;
            double[] b;
            double[] c;
            Owner.CalcTransMatrix(out a, out b, out c);

            double[][,] sNuN = new double[2][,];

            // sNxN
            sNuN[0] = new double[1, 1];
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    sNuN[0][i, j] = 0.0;
                }
            }

            // sNyN
            sNuN[1] = new double[1, 1];
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    sNuN[1][i, j] = 0.0;
                }
            }

            return sNuN;
        }
    }
}
