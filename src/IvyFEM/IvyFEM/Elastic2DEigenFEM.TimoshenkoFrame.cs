﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public partial class Elastic2DEigenFEM
    {
        protected void CalcTimoshenkoFrameElementKMForLine(
            uint feId, IvyFEM.Lapack.DoubleMatrix K, IvyFEM.Lapack.DoubleMatrix M)
        {
            {
                // Note: feIdは線要素
                uint quantityId0 = 0;
                LineFE workLineFE = World.GetLineFE(quantityId0, feId);
                uint workMaId = workLineFE.MaterialId;
                if (!World.IsMaterialId(workMaId))
                {
                    return;
                }
                Material workMa0 = World.GetMaterial(workMaId);
                if (!(workMa0 is TimoshenkoFrameMaterial))
                {
                    return;
                }
            }

            uint d1QuantityId = 0; // displacement
            uint d2QuantityId = 1; // displacement
            uint rQuantityId = 2; // rotation
            System.Diagnostics.Debug.Assert(World.GetDof(d1QuantityId) == 1);
            System.Diagnostics.Debug.Assert(World.GetDof(d2QuantityId) == 1);
            System.Diagnostics.Debug.Assert(World.GetDof(rQuantityId) == 1);
            int d1Dof = 1; // u
            int d2Dof = 1; // v
            int rDof = 1; //θ
            int d1NodeCnt = (int)World.GetNodeCount(d1QuantityId);
            int d2NodeCnt = (int)World.GetNodeCount(d2QuantityId);
            int rNodeCnt = (int)World.GetNodeCount(rQuantityId);
            int d2Offset = d1NodeCnt * d1Dof;
            int rOffset = d2Offset + d2NodeCnt * d2Dof;

            // Note: feIdは線要素
            LineFE d1LineFE = World.GetLineFE(d1QuantityId, feId);
            LineFE d2LineFE = World.GetLineFE(d2QuantityId, feId);
            LineFE rLineFE = World.GetLineFE(rQuantityId, feId);
            uint maId = d1LineFE.MaterialId;
            if (!World.IsMaterialId(maId))
            {
                return;
            }
            Material ma0 = World.GetMaterial(maId);
            System.Diagnostics.Debug.Assert(ma0 is TimoshenkoFrameMaterial);

            int[] d1CoIds = d1LineFE.NodeCoordIds;
            uint d1ElemNodeCnt = d1LineFE.NodeCount;
            // 暫定で要素マトリクスをこのメソッド内に直接記述
            System.Diagnostics.Debug.Assert(d1ElemNodeCnt == 2); // 1次要素
            int[] d1Nodes = new int[d1ElemNodeCnt];
            for (int iNode = 0; iNode < d1ElemNodeCnt; iNode++)
            {
                int coId = d1CoIds[iNode];
                int nodeId = World.Coord2Node(d1QuantityId, coId);
                d1Nodes[iNode] = nodeId;
            }
            int[] d2CoIds = d2LineFE.NodeCoordIds;
            uint d2ElemNodeCnt = d2LineFE.NodeCount;
            int[] d2Nodes = new int[d2ElemNodeCnt];
            for (int iNode = 0; iNode < d2ElemNodeCnt; iNode++)
            {
                int coId = d2CoIds[iNode];
                int nodeId = World.Coord2Node(d2QuantityId, coId);
                d2Nodes[iNode] = nodeId;
            }
            int[] rCoIds = rLineFE.NodeCoordIds;
            uint rElemNodeCnt = rLineFE.NodeCount;
            int[] rNodes = new int[rElemNodeCnt];
            for (int iNode = 0; iNode < rElemNodeCnt; iNode++)
            {
                int coId = rCoIds[iNode];
                int nodeId = World.Coord2Node(rQuantityId, coId);
                rNodes[iNode] = nodeId;
            }
            int[] vCoIds = d1LineFE.VertexCoordIds;
            uint elemVertexCnt = d1LineFE.VertexCount;
            double[][] vCoords = new double[elemVertexCnt][];
            for (int iVertex = 0; iVertex < elemVertexCnt; iVertex++)
            {
                int coId = vCoIds[iVertex];
                double[] coord = World.GetCoord(d1QuantityId, coId);
                vCoords[iVertex] = coord;
            }

            var ma = ma0 as TimoshenkoFrameMaterial;
            double Ae = ma.Area;
            double Iz = ma.SecondMomentOfArea;
            double rho = ma.MassDensity;
            double E = ma.Young;
            double G = ma.ShearCoefficient;
            double kappa = ma.ShearCorrectionFactor;

            // local dof
            double[] pt1 = vCoords[0];
            double[] pt2 = vCoords[1];
            double le = Math.Sqrt(
                (pt2[0] - pt1[0]) * (pt2[0] - pt1[0]) +
                (pt2[1] - pt1[1]) * (pt2[1] - pt1[1]));
            double cosXX = (pt2[0] - pt1[0]) / le;
            double cosXY = (pt2[1] - pt1[1]) / le;
            double cosYX = -1.0 * (pt2[1] - pt1[1]) / le;
            double cosYY = (pt2[0] - pt1[0]) / le;

            // local dof
            int localDof = 3;
            System.Diagnostics.Debug.Assert(localDof == (d1Dof + d2Dof + rDof));
            int localD2Offset = d1Dof;
            int localROffset = localD2Offset + d2Dof;
            // 次数の高い要素に合わせる
            System.Diagnostics.Debug.Assert(d1ElemNodeCnt == d2ElemNodeCnt);
            int localMaxNodeCnt = (int)Math.Max(Math.Max(d1ElemNodeCnt, d2ElemNodeCnt), rElemNodeCnt);
            var T = new IvyFEM.Lapack.DoubleMatrix(localMaxNodeCnt * localDof, localMaxNodeCnt * localDof);
            // d1
            for (int row = 0; row < d1ElemNodeCnt; row++)
            {
                // d1
                {
                    int col = row;
                    T[row * localDof, col * localDof] = cosXX;
                }
                // d2
                {
                    int col = row;
                    T[row * localDof, col * localDof + localD2Offset] = cosXY;
                }
                // r
                {
                    int col = row;
                    T[row * localDof, col * localDof + localROffset] = 0.0;
                }
            }
            // d2
            for (int row = 0; row < d2ElemNodeCnt; row++)
            {
                // d1
                {
                    int col = row;
                    T[row * localDof + localD2Offset, col * localDof] = cosYX;
                }
                // d2
                {
                    int col = row;
                    T[row * localDof + localD2Offset, col * localDof + localD2Offset] = cosYY;
                }
                // r
                {
                    int col = row;
                    T[row * localDof + localD2Offset, col * localDof + localROffset] = 0.0;
                }
            }
            // r
            for (int row = 0; row < rElemNodeCnt; row++)
            {
                // d1
                {
                    int col = row;
                    T[row * localDof + localROffset, col * localDof] = 0.0;
                }
                // d2
                {
                    int col = row;
                    T[row * localDof + localROffset, col * localDof + localD2Offset] = 0.0;
                }
                // r
                {
                    int col = row;
                    T[row * localDof + localROffset, col * localDof + localROffset] = 1.0;
                }
            }
            var transT = IvyFEM.Lapack.DoubleMatrix.Transpose(T);

            // Truss
            var localKeTruss = CalcTrussLocalKe(le, E, Ae);
            var localMeTruss = CalcTrussLocalMe(le, rho, Ae);

            // Timoshenko Beam
            var localKeBeam = CalcTimoshenkoBeamKl(
                d2ElemNodeCnt, rElemNodeCnt, d2Dof, rDof, d2LineFE, rLineFE,
                E, G, kappa, Ae, Iz);
            var localMeBeam = CalcTimoshenkoBeamMl(
                d2ElemNodeCnt, rElemNodeCnt, d2Dof, rDof, d2LineFE, rLineFE,
                rho, Ae, Iz);

            int beamDof = 2;
            int localBeamROffset = 1;
            System.Diagnostics.Debug.Assert(beamDof == (d2Dof + rDof));
            System.Diagnostics.Debug.Assert(localBeamROffset == d2Dof);

            var localKe = new IvyFEM.Lapack.DoubleMatrix(localMaxNodeCnt * localDof, localMaxNodeCnt * localDof);
            var localMe = new IvyFEM.Lapack.DoubleMatrix(localMaxNodeCnt * localDof, localMaxNodeCnt * localDof);
            // d1
            for (int row = 0; row < d1ElemNodeCnt; row++)
            {
                // d1
                for (int col = 0; col < d1ElemNodeCnt; col++)
                {
                    localKe[row * localDof, col * localDof] = localKeTruss[row * d1Dof, col * d1Dof];
                    localMe[row * localDof, col * localDof] = localMeTruss[row * d1Dof, col * d1Dof];
                }
                // d2
                for (int col = 0; col < d2ElemNodeCnt; col++)
                {
                    localKe[row * localDof, col * localDof + localD2Offset] = 0.0;
                    localMe[row * localDof, col * localDof + localD2Offset] = 0.0;
                }
                // r
                for (int col = 0; col < rElemNodeCnt; col++)
                {
                    localKe[row * localDof, col * localDof + localROffset] = 0.0;
                    localMe[row * localDof, col * localDof + localROffset] = 0.0;
                }
            }
            // d2
            for (int row = 0; row < d2ElemNodeCnt; row++)
            {
                // d1
                for (int col = 0; col < d1ElemNodeCnt; col++)
                {
                    localKe[row * localDof + localD2Offset, col * localDof] = 0.0;
                    localMe[row * localDof + localD2Offset, col * localDof] = 0.0;
                }
                // d2
                for (int col = 0; col < d2ElemNodeCnt; col++)
                {
                    localKe[row * localDof + localD2Offset, col * localDof + localD2Offset] =
                        localKeBeam[row * (d2Dof + rDof), col * (d2Dof + rDof)];
                    localMe[row * localDof + localD2Offset, col * localDof + localD2Offset] =
                        localMeBeam[row * (d2Dof + rDof), col * (d2Dof + rDof)];
                }
                // r
                for (int col = 0; col < rElemNodeCnt; col++)
                {
                    localKe[row * localDof + localD2Offset, col * localDof + localROffset] =
                        localKeBeam[row * (d2Dof + rDof), col * (d2Dof + rDof) + localBeamROffset];
                    localMe[row * localDof + localD2Offset, col * localDof + localROffset] =
                        localMeBeam[row * (d2Dof + rDof), col * (d2Dof + rDof) + localBeamROffset];
                }
            }
            // r
            for (int row = 0; row < rElemNodeCnt; row++)
            {
                // d1
                for (int col = 0; col < d1ElemNodeCnt; col++)
                {
                    localKe[row * localDof + localROffset, col * localDof] = 0.0;
                    localMe[row * localDof + localROffset, col * localDof] = 0.0;
                }
                // d2
                for (int col = 0; col < d2ElemNodeCnt; col++)
                {
                    localKe[row * localDof + localROffset, col * localDof + localD2Offset] =
                        localKeBeam[row * (d2Dof + rDof) + localBeamROffset, col * (d2Dof + rDof)];
                    localMe[row * localDof + localROffset, col * localDof + localD2Offset] =
                        localMeBeam[row * (d2Dof + rDof) + localBeamROffset, col * (d2Dof + rDof)];
                }
                // r
                for (int col = 0; col < rElemNodeCnt; col++)
                {
                    localKe[row * localDof + localROffset, col * localDof + localROffset] =
                        localKeBeam[row * (d2Dof + rDof) + localBeamROffset, col * (d2Dof + rDof) + localBeamROffset];
                    localMe[row * localDof + localROffset, col * localDof + localROffset] =
                        localMeBeam[row * (d2Dof + rDof) + localBeamROffset, col * (d2Dof + rDof) + localBeamROffset];
                }
            }

            var Ke = (transT * localKe) * T;
            var Me = (transT * localMe) * T;

            // displacement 1
            for (int row = 0; row < d1ElemNodeCnt; row++)
            {
                int rowNodeId = d1Nodes[row];
                if (rowNodeId == -1)
                {
                    continue;
                }
                // displacement 1
                for (int col = 0; col < d1ElemNodeCnt; col++)
                {
                    int colNodeId = d1Nodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof, col * localDof];
                    double mValue = Me[row * localDof, col * localDof];
                    K[rowNodeId, colNodeId] += kValue;
                    M[rowNodeId, colNodeId] += mValue;
                }
                // displacement 2
                for (int col = 0; col < d2ElemNodeCnt; col++)
                {
                    int colNodeId = d2Nodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof, col * localDof + localD2Offset];
                    double mValue = Me[row * localDof, col * localDof + localD2Offset];
                    K[rowNodeId, colNodeId + d2Offset] += kValue;
                    M[rowNodeId, colNodeId + d2Offset] += mValue;
                }
                // rotation
                for (int col = 0; col < rElemNodeCnt; col++)
                {
                    int colNodeId = rNodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof, col * localDof + localROffset];
                    double mValue = Me[row * localDof, col * localDof + localROffset];
                    K[rowNodeId, colNodeId + rOffset] += kValue;
                    M[rowNodeId, colNodeId + rOffset] += mValue;
                }
            }
            // displacement 2
            for (int row = 0; row < d2ElemNodeCnt; row++)
            {
                int rowNodeId = d2Nodes[row];
                if (rowNodeId == -1)
                {
                    continue;
                }
                // displacement 1
                for (int col = 0; col < d1ElemNodeCnt; col++)
                {
                    int colNodeId = d1Nodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof + localD2Offset, col * localDof];
                    double mValue = Me[row * localDof + localD2Offset, col * localDof];
                    K[rowNodeId + d2Offset, colNodeId] += kValue;
                    M[rowNodeId + d2Offset, colNodeId] += mValue;
                }
                // displacement 2
                for (int col = 0; col < d2ElemNodeCnt; col++)
                {
                    int colNodeId = d2Nodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof + localD2Offset, col * localDof + localD2Offset];
                    double mValue = Me[row * localDof + localD2Offset, col * localDof + localD2Offset];
                    K[rowNodeId + d2Offset, colNodeId + d2Offset] += kValue;
                    M[rowNodeId + d2Offset, colNodeId + d2Offset] += mValue;
                }
                // rotation
                for (int col = 0; col < rElemNodeCnt; col++)
                {
                    int colNodeId = rNodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof + localD2Offset, col * localDof + localROffset];
                    double mValue = Me[row * localDof + localD2Offset, col * localDof + localROffset];
                    K[rowNodeId + d2Offset, colNodeId + rOffset] += kValue;
                    M[rowNodeId + d2Offset, colNodeId + rOffset] += mValue;
                }
            }
            // rotation
            for (int row = 0; row < rElemNodeCnt; row++)
            {
                int rowNodeId = rNodes[row];
                if (rowNodeId == -1)
                {
                    continue;
                }
                // displacement 1
                for (int col = 0; col < d1ElemNodeCnt; col++)
                {
                    int colNodeId = d1Nodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof + localROffset, col * localDof];
                    double mValue = Me[row * localDof + localROffset, col * localDof];
                    K[rowNodeId + rOffset, colNodeId] += kValue;
                    M[rowNodeId + rOffset, colNodeId] += mValue;
                }
                // displacement 2
                for (int col = 0; col < d2ElemNodeCnt; col++)
                {
                    int colNodeId = d2Nodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof + localROffset, col * localDof + localD2Offset];
                    double mValue = Me[row * localDof + localROffset, col * localDof + localD2Offset];
                    K[rowNodeId + rOffset, colNodeId + d2Offset] += kValue;
                    M[rowNodeId + rOffset, colNodeId + d2Offset] += mValue;
                }
                // rotation
                for (int col = 0; col < rElemNodeCnt; col++)
                {
                    int colNodeId = rNodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    double kValue = Ke[row * localDof + localROffset, col * localDof + localROffset];
                    double mValue = Me[row * localDof + localROffset, col * localDof + localROffset];
                    K[rowNodeId + rOffset, colNodeId + rOffset] += kValue;
                    M[rowNodeId + rOffset, colNodeId + rOffset] += mValue;
                }
            }
        }
    }
}
