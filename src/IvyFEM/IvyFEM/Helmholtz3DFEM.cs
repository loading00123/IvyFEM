﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public class Helmholtz3DFEM : FEM
    {
        // input
        public double Frequency { get; set; }

        // output
        public System.Numerics.Complex[] U { get; private set; } = null;

        public Helmholtz3DFEM(FEWorld world)
        {
            World = world;
        }

        private void CalcAB(IvyFEM.Linear.ComplexSparseMatrix A, System.Numerics.Complex[] B)
        {
            uint quantityId = 0;
            IList<uint> feIds = World.GetTetrahedronFEIds(quantityId);
            foreach (uint feId in feIds)
            {
                TetrahedronFE tetFE = World.GetTetrahedronFE(quantityId, feId);
                uint elemNodeCnt = tetFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = tetFE.NodeCoordIds[iNode];
                    int nodeId = World.Coord2Node(quantityId, coId);
                    nodes[iNode] = nodeId;
                }

                Material ma0 = World.GetMaterial(tetFE.MaterialId);
                System.Diagnostics.Debug.Assert(ma0 is HelmholtzMaterial);
                var ma = ma0 as HelmholtzMaterial;
                double v = ma.Velocity;
                System.Numerics.Complex f = ma.F;
                double omega = 2.0 * Math.PI * Frequency;
                double k = omega / v;

                double[,] sNN = tetFE.CalcSNN();
                double[,][,] sNuNv = tetFE.CalcSNuNv();
                double[,] sNxNx = sNuNv[0, 0];
                double[,] sNyNy = sNuNv[1, 1];
                double[,] sNzNz = sNuNv[2, 2];
                double[] sN = tetFE.CalcSN();
                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    for (int col = 0; col < elemNodeCnt; col++)
                    {
                        int colNodeId = nodes[col];
                        if (colNodeId == -1)
                        {
                            continue;
                        }
                        double a = sNxNx[row, col] + sNyNy[row, col] + sNzNz[row, col] - k * k * sNN[row, col];

                        A[rowNodeId, colNodeId] += a;
                    }
                }
                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    System.Numerics.Complex b = f * sN[row];
                    B[rowNodeId] += b;
                }
            }
        }

        private void SetABC(IvyFEM.Linear.ComplexSparseMatrix A, System.Numerics.Complex[] B)
        {
            uint quantityId = 0;
            if (World.GetPortCount(quantityId) ==  0)
            {
                return;
            }
            System.Diagnostics.Debug.Assert(World.GetPortCount(quantityId) == 1);
            IList<PortCondition> portConditions = World.GetPortConditions(quantityId);
            PortCondition portCondition = portConditions[0];
            IList<uint> abcLIds = portCondition.LIds;
            IList<uint> feIds = World.GetTriangleFEIds(quantityId);
            foreach (uint feId in feIds)
            {
                TriangleFE triFE = World.GetTriangleFE(quantityId, feId);
                uint meshId = triFE.MeshId;
                uint lId;
                {
                    uint elemCount;
                    MeshType meshType;
                    int loc;
                    uint cadId;
                    World.Mesh.GetMeshInfo(meshId, out elemCount, out meshType, out loc, out cadId);
                    System.Diagnostics.Debug.Assert(meshType == MeshType.Tri);
                    lId = cadId;
                }
                if (abcLIds.Contains(lId))
                {
                    // ABCを適用する辺
                }
                else
                {
                    continue;
                }

                // ABC
                uint elemNodeCnt = triFE.NodeCount;
                int[] coIds = triFE.NodeCoordIds;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = coIds[iNode];
                    nodes[iNode] = World.Coord2Node(quantityId, coId);
                }
                Material ma0 = World.GetMaterial(triFE.MaterialId);
                System.Diagnostics.Debug.Assert(ma0 is HelmholtzMaterial);
                var ma = ma0 as HelmholtzMaterial;
                double v = ma.Velocity;
                //System.Numerics.Complex f = ma.F;
                double omega = 2.0 * Math.PI * Frequency;
                double k = omega / v;

                double[,] sNN = triFE.CalcSNN();
                double[,][,] sNuNv = triFE.CalcSNuNv();
                double[,] sNxNx = sNuNv[0, 0];
                double[,] sNyNy = sNuNv[1, 1];
                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    for (int col = 0; col < elemNodeCnt; col++)
                    {
                        int colNodeId = nodes[col];
                        if (colNodeId == -1)
                        {
                            continue;
                        }

                        System.Numerics.Complex a =
                            System.Numerics.Complex.ImaginaryOne * k * sNN[row, col] -
                            System.Numerics.Complex.ImaginaryOne / (2.0 * k) * (sNxNx[row, col] + sNyNy[row, col]);
                        A[rowNodeId, colNodeId] += a;
                    }
                }
            }
        }

        public override void Solve()
        {
            uint quantityId = 0;
            int nodeCnt = (int)World.GetNodeCount(quantityId);
            var A = new IvyFEM.Linear.ComplexSparseMatrix(nodeCnt, nodeCnt);
            var B = new System.Numerics.Complex[nodeCnt];
            CalcAB(A, B);

            SetABC(A, B);

            ComplexSetFixedCadsCondtion(A, B);

            System.Numerics.Complex[] X;
            Solver.ComplexSolve(out X, A, B);
            U = X;
        }
    }
}
