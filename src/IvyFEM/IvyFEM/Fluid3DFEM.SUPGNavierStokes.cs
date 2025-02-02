﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public partial class Fluid3DFEM
    {
        private void CalcSUPGNavierStokesAB(IvyFEM.Linear.DoubleSparseMatrix A, double[] B)
        {
            uint vQuantityId = 0;
            uint pQuantityId = 1;
            int vDof = 3;
            int pDof = 1;
            int vNodeCnt = (int)World.GetNodeCount(vQuantityId);
            int pNodeCnt = (int)World.GetNodeCount(pQuantityId);
            int offset = vNodeCnt * vDof;

            IList<uint> feIds = World.GetTetrahedronFEIds(vQuantityId);
            foreach (uint feId in feIds)
            {
                TetrahedronFE vTetFE = World.GetTetrahedronFE(vQuantityId, feId);
                TetrahedronFE pTetFE = World.GetTetrahedronFE(pQuantityId, feId);
                uint vertexCnt = vTetFE.VertexCount;
                for (int iVertex = 0; iVertex < vertexCnt; iVertex++)
                {
                    System.Diagnostics.Debug.Assert(vTetFE.VertexCoordIds[iVertex] == pTetFE.VertexCoordIds[iVertex]);
                }

                int[] vCoIds = vTetFE.NodeCoordIds;
                uint vElemNodeCnt = vTetFE.NodeCount;
                int[] vNodes = new int[vElemNodeCnt];
                for (int iNode = 0; iNode < vElemNodeCnt; iNode++)
                {
                    int coId = vCoIds[iNode];
                    int nodeId = World.Coord2Node(vQuantityId, coId);
                    vNodes[iNode] = nodeId;
                }
                int[] pCoIds = pTetFE.NodeCoordIds;
                uint pElemNodeCnt = pTetFE.NodeCount;
                int[] pNodes = new int[pElemNodeCnt];
                for (int iNode = 0; iNode < pElemNodeCnt; iNode++)
                {
                    int coId = pCoIds[iNode];
                    int nodeId = World.Coord2Node(pQuantityId, coId);
                    pNodes[iNode] = nodeId;
                }

                Material ma0 = World.GetMaterial(vTetFE.MaterialId);
                System.Diagnostics.Debug.Assert(ma0 is NewtonFluidMaterial);
                var ma = ma0 as NewtonFluidMaterial;
                double rho = ma.MassDensity;
                double mu = ma.Mu;
                double nu = mu / rho;
                double[] g = { ma.GravityX, ma.GravityY, ma.GravityZ };

                double[][] velos = new double[vElemNodeCnt][];
                for (int iNode = 0; iNode < vElemNodeCnt; iNode++)
                {
                    double[] velo = new double[vDof];
                    int nodeId = vNodes[iNode];
                    if (nodeId == -1)
                    {
                        // 0
                    }
                    else
                    {
                        for (int iDof = 0; iDof < vDof; iDof++)
                        {
                            velo[iDof] = U[nodeId * vDof + iDof];
                        }
                    }
                    velos[iNode] = velo;
                }

                double taum = 0;
                double tauc = 0;
                {
                    double[] aveVelo = {
                        (velos[0][0] + velos[1][0] + velos[2][0]) / 4.0,
                        (velos[0][1] + velos[1][1] + velos[2][1]) / 4.0,
                        (velos[0][2] + velos[1][2] + velos[2][2]) / 4.0
                    };
                    double veloNorm = Math.Sqrt(
                        aveVelo[0] * aveVelo[0] + aveVelo[1] * aveVelo[1] + aveVelo[2] * aveVelo[2]);
                    double[][] Lu = new double[vDof][];
                    {
                        double[] a;
                        double[] b;
                        double[] c;
                        double[] d;
                        vTetFE.CalcTransMatrix(out a, out b, out c, out d);
                        // Lx
                        Lu[0] = b;
                        // Ly
                        Lu[1] = c;
                        // Lz
                        Lu[2] = d;
                    }
                    IvyFEM.Lapack.DoubleMatrix GMat = new IvyFEM.Lapack.DoubleMatrix(vDof, vDof);
                    double[] gVec = new double[vDof];
                    for (int iDof = 0; iDof < vDof; iDof++)
                    {
                        for (int jDof = 0; jDof < vDof; jDof++)
                        {
                            for (int kDof = 0; kDof < vDof; kDof++)
                            {
                                GMat[iDof, jDof] += Lu[iDof][kDof] * Lu[jDof][kDof];
                            }
                        }
                    }
                    for (int iDof = 0; iDof < vDof; iDof++)
                    {
                        for (int kDof = 0; kDof < vDof; kDof++)
                        {
                            gVec[iDof] += Lu[iDof][kDof];
                        }
                    }

                    double sqinvtaum1 = 0;
                    double sqinvtaum2 = 0;
                    {
                        double[] tmpVec = GMat * aveVelo;
                        sqinvtaum2 = IvyFEM.Lapack.Functions.ddot(aveVelo, tmpVec);
                    }
                    double sqinvtaum3 = 0;
                    {
                        IvyFEM.Lapack.DoubleMatrix GMatT = new Lapack.DoubleMatrix(GMat);
                        GMatT.Transpose();
                        double GMatDoubleDot = IvyFEM.Lapack.DoubleMatrix.DoubleDot(GMat, GMatT);
                        sqinvtaum3 = 30.0 * nu * nu * GMatDoubleDot;
                    }
                    double sqinvtaum = sqinvtaum1 + sqinvtaum2 + sqinvtaum3;
                    taum = 1.0 / Math.Sqrt(sqinvtaum);

                    double gDot = IvyFEM.Lapack.Functions.ddot(gVec, gVec);
                    tauc = 1.0 / (taum * gDot);
                }

                double[] vSN = vTetFE.CalcSN();
                IntegrationPoints ip = TetrahedronFE.GetIntegrationPoints(World.TetIntegrationPointCount);
                for (int ipPt = 0; ipPt < ip.PointCount; ipPt++)
                {
                    double[] L = ip.Ls[ipPt];
                    double[] vN = vTetFE.CalcN(L);
                    double[][] vNu = vTetFE.CalcNu(L);
                    double[] vNx = vNu[0];
                    double[] vNy = vNu[1];
                    double[] vNz = vNu[2];
                    double[,][] vNuv = vTetFE.CalcNuv(L);
                    double[] pN = pTetFE.CalcN(L);
                    double[][] pNu = pTetFE.CalcNu(L);
                    double[] pNx = pNu[0];
                    double[] pNy = pNu[1];
                    double[] pNz = pNu[2];

                    double detJ = vTetFE.GetDetJacobian(L);
                    double weight = ip.Weights[ipPt];
                    double detJWeight = (1.0 / 6.0) * weight * detJ;

                    double[] v = new double[vDof];
                    double[] vx = new double[vDof];
                    double[] vy = new double[vDof];
                    double[] vz = new double[vDof];
                    double[] vxx = new double[vDof];
                    double[] vxy = new double[vDof];
                    double[] vxz = new double[vDof];
                    double[] vyx = new double[vDof];
                    double[] vyy = new double[vDof];
                    double[] vyz = new double[vDof];
                    double[] vzx = new double[vDof];
                    double[] vzy = new double[vDof];
                    double[] vzz = new double[vDof];
                    double p = 0;
                    double px = 0;
                    double py = 0;
                    double pz = 0;
                    for (int iNode = 0; iNode < vElemNodeCnt; iNode++)
                    {
                        int nodeId = vNodes[iNode];
                        if (nodeId == -1)
                        {
                            continue;
                        }
                        for (int iDof = 0; iDof < vDof; iDof++)
                        {
                            double vValue = U[nodeId * vDof + iDof];
                            v[iDof] += vValue * vN[iNode];
                            vx[iDof] += vValue * vNx[iNode];
                            vy[iDof] += vValue * vNy[iNode];
                            vz[iDof] += vValue * vNz[iNode];
                            vxx[iDof] += vValue * vNuv[0, 0][iNode];
                            vxy[iDof] += vValue * vNuv[0, 1][iNode];
                            vxz[iDof] += vValue * vNuv[0, 2][iNode];
                            vyx[iDof] += vValue * vNuv[1, 0][iNode];
                            vyy[iDof] += vValue * vNuv[1, 1][iNode];
                            vyz[iDof] += vValue * vNuv[1, 2][iNode];
                            vzx[iDof] += vValue * vNuv[2, 0][iNode];
                            vzy[iDof] += vValue * vNuv[2, 1][iNode];
                            vzz[iDof] += vValue * vNuv[2, 2][iNode];
                        }
                    }
                    for (int iNode = 0; iNode < pElemNodeCnt; iNode++)
                    {
                        int nodeId = pNodes[iNode];
                        if (nodeId == -1)
                        {
                            continue;
                        }
                        double pValue = U[offset + nodeId];
                        p += pValue * pN[iNode];
                        px += pValue * pNx[iNode];
                        py += pValue * pNy[iNode];
                        pz += pValue * pNz[iNode];
                    }
                    double[][] vu = new double[vDof][];
                    vu[0] = vx;
                    vu[1] = vy;
                    vu[2] = vz;
                    double[,][] vuv = new double[vDof, vDof][];
                    vuv[0, 0] = vxx;
                    vuv[0, 1] = vxy;
                    vuv[0, 2] = vxz;
                    vuv[1, 0] = vyx;
                    vuv[1, 1] = vyy;
                    vuv[1, 2] = vyz;
                    vuv[2, 0] = vzx;
                    vuv[2, 1] = vzy;
                    vuv[2, 2] = vzz;
                    double[] pu = new double[vDof];
                    pu[0] = px;
                    pu[1] = py;
                    pu[2] = pz;

                    for (int row = 0; row < vElemNodeCnt; row++)
                    {
                        int rowNodeId = vNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int col = 0; col < vElemNodeCnt; col++)
                        {
                            int colNodeId = vNodes[col];
                            if (colNodeId == -1)
                            {
                                continue;
                            }

                            double[,] kvv1 = new double[vDof, vDof];
                            kvv1[0, 0] = detJWeight * mu * (vNx[row] * vNx[col] +
                                vNx[row] * vNx[col] + vNy[row] * vNy[col] + vNz[row] * vNz[col]);
                            kvv1[0, 1] = detJWeight * mu * vNy[row] * vNx[col];
                            kvv1[0, 2] = detJWeight * mu * vNz[row] * vNx[col];
                            kvv1[1, 0] = detJWeight * mu * vNx[row] * vNy[col];
                            kvv1[1, 1] = detJWeight * mu * (vNy[row] * vNy[col] +
                                vNx[row] * vNx[col] + vNy[row] * vNy[col] + vNz[row] * vNz[col]);
                            kvv1[1, 2] = detJWeight * mu * vNz[row] * vNy[col];
                            kvv1[2, 0] = detJWeight * mu * vNx[row] * vNz[col];
                            kvv1[2, 1] = detJWeight * mu * vNy[row] * vNz[col];
                            kvv1[2, 2] = detJWeight * mu * (vNz[row] * vNz[col] +
                                vNx[row] * vNx[col] + vNy[row] * vNy[col] + vNz[row] * vNz[col]);

                            double[,] kvv2 = new double[vDof, vDof];
                            kvv2[0, 0] = detJWeight * rho * vN[row] * (
                                v[0] * vNx[col] + v[1] * vNy[col] + v[2] * vNz[col]);
                            kvv2[0, 1] = 0;
                            kvv2[0, 2] = 0;
                            kvv2[1, 0] = 0;
                            kvv2[1, 1] = detJWeight * rho * vN[row] * (
                                v[0] * vNx[col] + v[1] * vNy[col] + v[2] * vNz[col]);
                            kvv2[1, 2] = 0;
                            kvv2[2, 0] = 0;
                            kvv2[2, 1] = 0;
                            kvv2[2, 2] = detJWeight * rho * vN[row] * (
                                v[0] * vNx[col] + v[1] * vNy[col] + v[2] * vNz[col]);

                            for (int rowDof = 0; rowDof < vDof; rowDof++)
                            {
                                for (int colDof = 0; colDof < vDof; colDof++)
                                {
                                    A[rowNodeId * vDof + rowDof, colNodeId * vDof + colDof] +=
                                        kvv1[rowDof, colDof] + kvv2[rowDof, colDof];
                                }
                            }
                        }
                    }

                    for (int row = 0; row < vElemNodeCnt; row++)
                    {
                        int rowNodeId = vNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int col = 0; col < pElemNodeCnt; col++)
                        {
                            int colNodeId = pNodes[col];
                            if (colNodeId == -1)
                            {
                                continue;
                            }

                            double[,] kvp = new double[vDof, pDof];
                            kvp[0, 0] = -detJWeight * vNx[row] * pN[col];
                            kvp[1, 0] = -detJWeight * vNy[row] * pN[col];
                            kvp[2, 0] = -detJWeight * vNz[row] * pN[col];

                            for (int rowDof = 0; rowDof < vDof; rowDof++)
                            {
                                A[rowNodeId * vDof + rowDof, offset + colNodeId] += kvp[rowDof, 0];
                                A[offset + colNodeId, rowNodeId * vDof + rowDof] += -kvp[rowDof, 0];
                            }
                        }
                    }

                    //////////////////////////////////////////////////////////////
                    // SUPG
                    double[] rmi = new double[vDof];
                    double[,,] rmivj = new double[vDof, vDof, vElemNodeCnt];
                    double[,] rmip = new double[vDof, pElemNodeCnt];
                    double rc = 0;
                    double[,] rcvj = new double[vDof, vElemNodeCnt];
                    for (int iDof = 0; iDof < vDof; iDof++)
                    {
                        rmi[iDof] =
                            -mu * (vuv[0, 0][iDof] + vuv[1, 1][iDof] + vuv[2, 2][iDof]) +
                            rho * (v[0] * vx[iDof] + v[1] * vy[iDof] + v[2] * vz[iDof]) +
                            pu[iDof] - rho * g[iDof];

                        for (int jDof = 0; jDof < vDof; jDof++)
                        {
                            for (int jNode = 0; jNode < vElemNodeCnt; jNode++)
                            {
                                int jNodeId = vNodes[jNode];
                                if (jNodeId == -1)
                                {
                                    continue;
                                }

                                rmivj[iDof, jDof, jNode] = 0;
                                if (iDof == jDof)
                                {
                                    rmivj[iDof, jDof, jNode] +=
                                        -mu * (vNuv[0, 0][jNode] + vNuv[1, 1][jNode] + vNuv[2, 2][jNode]);
                                }
                                if (iDof == jDof)
                                {
                                    rmivj[iDof, jDof, jNode] +=
                                        rho * (v[0] * vNu[0][jNode] + v[1] * vNu[1][jNode] + v[2] * vNu[2][jNode]);
                                }
                            }
                        }

                        for (int jNode = 0; jNode < pElemNodeCnt; jNode++)
                        {
                            int jNodeId = pNodes[jNode];
                            if (jNodeId == -1)
                            {
                                continue;
                            }
                            rmip[iDof, jNode] = pNu[iDof][jNode];
                        }
                    }
                    {
                        rc = vx[0] + vy[1] + vz[2];
                        for (int jDof = 0; jDof < vDof; jDof++)
                        {
                            for (int jNode = 0; jNode < vElemNodeCnt; jNode++)
                            {
                                int jNodeId = vNodes[jNode];
                                if (jNodeId == -1)
                                {
                                    continue;
                                }
                                rcvj[jDof, jNode] = vNu[jDof][jNode];
                            }
                        }
                    }
                    // kvv
                    for (int row = 0; row < vElemNodeCnt; row++)
                    {
                        int rowNodeId = vNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int col = 0; col < vElemNodeCnt; col++)
                        {
                            int colNodeId = vNodes[col];
                            if (colNodeId == -1)
                            {
                                continue;
                            }

                            double[,] kvv1 = new double[vDof, vDof];
                            double[,] kvv2 = new double[vDof, vDof];
                            for (int rowDof = 0; rowDof < vDof; rowDof++)
                            {
                                for (int colDof = 0; colDof < vDof; colDof++)
                                {
                                    kvv1[rowDof, colDof] =
                                        detJWeight * taum * (
                                        v[0] * vNu[0][row] + v[1] * vNu[1][row] + v[2] * vNu[2][row]) *
                                        rmivj[rowDof, colDof, col];
                                    kvv2[rowDof, colDof] =
                                        detJWeight * tauc * rho * vNu[rowDof][row] * rcvj[colDof, col];
                                }
                            }

                            for (int rowDof = 0; rowDof < vDof; rowDof++)
                            {
                                for (int colDof = 0; colDof < vDof; colDof++)
                                {
                                    A[rowNodeId * vDof + rowDof, colNodeId * vDof + colDof] +=
                                        kvv1[rowDof, colDof] + kvv2[rowDof, colDof];
                                }
                            }
                        }
                    }
                    // kvp
                    for (int row = 0; row < vElemNodeCnt; row++)
                    {
                        int rowNodeId = vNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int col = 0; col < pElemNodeCnt; col++)
                        {
                            int colNodeId = pNodes[col];
                            if (colNodeId == -1)
                            {
                                continue;
                            }

                            double[,] kvp = new double[vDof, pDof];
                            for (int rowDof = 0; rowDof < vDof; rowDof++)
                            {
                                kvp[rowDof, 0] =
                                    detJWeight * taum *
                                    (v[0] * vNu[0][row] + v[1] * vNu[1][row] + v[2] * vNu[2][row]) * rmip[rowDof, col];
                            }

                            for (int rowDof = 0; rowDof < vDof; rowDof++)
                            {
                                A[rowNodeId * vDof + rowDof, offset + colNodeId] += kvp[rowDof, 0];
                            }
                        }
                    }
                    // kpv
                    for (int row = 0; row < pElemNodeCnt; row++)
                    {
                        int rowNodeId = pNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int col = 0; col < vElemNodeCnt; col++)
                        {
                            int colNodeId = vNodes[col];
                            if (colNodeId == -1)
                            {
                                continue;
                            }

                            double[,] kpv = new double[pDof, vDof];
                            for (int colDof = 0; colDof < vDof; colDof++)
                            {
                                kpv[0, colDof] =
                                    detJWeight * (1.0 / rho) * taum *
                                    (pNu[0][row] * rmivj[0, colDof, col] + pNu[1][row] * rmivj[1, colDof, col]
                                     + pNu[2][row] * rmivj[2, colDof, col]);
                            }

                            for (int colDof = 0; colDof < vDof; colDof++)
                            {
                                A[offset + rowNodeId, colNodeId * vDof + colDof] += kpv[0, colDof];
                            }
                        }
                    }
                    // kpp
                    for (int row = 0; row < pElemNodeCnt; row++)
                    {
                        int rowNodeId = pNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int col = 0; col < pElemNodeCnt; col++)
                        {
                            int colNodeId = pNodes[col];
                            if (colNodeId == -1)
                            {
                                continue;
                            }

                            double[,] kpp = new double[pDof, pDof];
                            kpp[0, 0] =
                                detJWeight * (1.0 / rho) * taum *
                                (pNu[0][row] * rmip[0, col] + pNu[1][row] * rmip[1, col] + pNu[2][row] * rmip[2, col]);

                            A[offset + rowNodeId, offset + colNodeId] += kpp[0, 0];
                        }
                    }

                    for (int row = 0; row < vElemNodeCnt; row++)
                    {
                        int rowNodeId = vNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        double[] qv1 = new double[vDof];
                        for (int rowDof = 0; rowDof < vDof; rowDof++)
                        {
                            qv1[rowDof] = detJWeight * taum *
                                (v[0] * vNu[0][row] + v[1] * vNu[1][row] + v[2] * vNu[2][row]) * (-rho * g[rowDof]);
                        }
                        for (int rowDof = 0; rowDof < vDof; rowDof++)
                        {
                            B[rowNodeId * vDof + rowDof] += -qv1[rowDof];
                        }
                    }

                    for (int row = 0; row < pElemNodeCnt; row++)
                    {
                        int rowNodeId = pNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        double[] qp = new double[pDof];
                        qp[0] = detJWeight * (1.0 / rho) * taum * 
                            (pNu[0][row] * (-rho * g[0]) + pNu[1][row] * (-rho * g[1]) + pNu[2][row] * (-rho * g[2]));
                        B[offset + rowNodeId] += -qp[0];
                    }
                }

                for (int row = 0; row < vElemNodeCnt; row++)
                {
                    int rowNodeId = vNodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    for (int rowDof = 0; rowDof < vDof; rowDof++)
                    {
                        B[rowNodeId * vDof + rowDof] += rho * g[rowDof] * vSN[row];
                    }
                }
            }
        }
    }
}
