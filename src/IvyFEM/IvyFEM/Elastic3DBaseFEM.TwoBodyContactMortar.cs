﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public partial class Elastic3DBaseFEM
    {
        private void SetTwoBodyContactMortarSegmentationQuantitySpecialBC(
            uint cQuantityId, IvyFEM.Linear.DoubleSparseMatrix A, double[] B)
        {
            uint uQuantityId = 0;
            System.Diagnostics.Debug.Assert(World.GetFEOrder(uQuantityId) == 1);
            System.Diagnostics.Debug.Assert(World.GetCoordCount(uQuantityId) ==
                World.GetCoordCount(cQuantityId));
            System.Diagnostics.Debug.Assert(World.GetDof(uQuantityId) == 3);
            System.Diagnostics.Debug.Assert(World.GetDof(cQuantityId) == 3);
            int uDof = 3;
            int cDof = 3;
            int uNodeCnt = (int)World.GetNodeCount(uQuantityId);
            int cNodeCnt = (int)World.GetNodeCount(cQuantityId);
            int offset = World.GetOffset(cQuantityId);

            // 要素の変位を更新
            World.UpdateFEDisplacements(uQuantityId, U);
            World.UpdateFEDisplacements(cQuantityId, U);

            // 節点法線ベクトルの計算
            Dictionary<int, double[]> co2Normal = GetSlaveTriangleFECo2Normal(uQuantityId, uDof, cQuantityId);
            bool[] lConstraintNodeIds = new bool[cNodeCnt];
            IList<uint> slaveFEIds = World.GetContactSlaveFEIds(cQuantityId);
            System.Diagnostics.Debug.Assert(slaveFEIds.Count > 0);

            foreach (uint slaveFEId in slaveFEIds)
            {
                TriangleFE triFE = World.GetTriangleFE(uQuantityId, slaveFEId);
                uint elemNodeCnt = triFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = triFE.NodeCoordIds[iNode];
                    int nodeId = World.Coord2Node(uQuantityId, coId);
                    nodes[iNode] = nodeId;
                }

                TriangleFE lTriFE = World.GetTriangleFE(cQuantityId, slaveFEId);
                int[] lNodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = lTriFE.NodeCoordIds[iNode];
                    int lNodeId = World.Coord2Node(cQuantityId, coId);
                    lNodes[iNode] = lNodeId;
                }

                double[][] curNodeCoord = new double[elemNodeCnt][];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = triFE.NodeCoordIds[iNode];
                    double[] coord = World.GetCoord(uQuantityId, coId);
                    int iNodeId = nodes[iNode];
                    curNodeCoord[iNode] = new double[uDof];
                    if (iNodeId == -1)
                    {
                        for (int iDof = 0; iDof < uDof; iDof++)
                        {
                            curNodeCoord[iNode][iDof] = coord[iDof];
                        }
                    }
                    else
                    {
                        for (int iDof = 0; iDof < uDof; iDof++)
                        {
                            curNodeCoord[iNode][iDof] = coord[iDof] + U[iNodeId * uDof + iDof];
                        }
                    }
                }

                IntegrationPoints ip = TriangleFE.GetIntegrationPoints(TriangleIntegrationPointCount.Point7);
                System.Diagnostics.Debug.Assert(ip.Ls.Length == 7);

                OpenTK.Vector3d[] slaveTriPts = new OpenTK.Vector3d[] {
                    new OpenTK.Vector3d(curNodeCoord[0][0], curNodeCoord[0][1], curNodeCoord[0][2]),
                    new OpenTK.Vector3d(curNodeCoord[1][0], curNodeCoord[1][1], curNodeCoord[1][2]),
                    new OpenTK.Vector3d(curNodeCoord[2][0], curNodeCoord[2][1], curNodeCoord[2][2]),
                };
                OpenTK.Vector3d slaveNormal;
                {
                    // 連続な法線ベクトルの三角形の重心の値で代表する
                    double[] L = { 1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0 };
                    double[] N = triFE.CalcN(L);
                    double[] normal = new double[uDof];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int coId = triFE.NodeCoordIds[iNode];
                        double[] nodeNormal = co2Normal[coId];
                        for (int iDof = 0; iDof < uDof; iDof++)
                        {
                            normal[iDof] += nodeNormal[iDof] * N[iNode];
                        }
                    }
                    normal = IvyFEM.Lapack.Utils.NormalizeDoubleVector(normal);
                    slaveNormal = new OpenTK.Vector3d(normal[0], normal[1], normal[2]);
                    /*
                    double[] insideCo = GetTetrahedronPointNotSharedByTriangle(triFE, uQuantityId, uDof);
                    double[] normal = triFE.GetNormal(insideCo);
                    slaveNormal = new OpenTK.Vector3d(normal[0], normal[1], normal[2]);
                    */
                }
                Dictionary<uint, IList<OpenTK.Vector3d[]>> masterFEIdSlaveSegments =
                    GetSlaveSegments(slaveTriPts, slaveNormal, uQuantityId, uDof, cQuantityId);
                foreach (var pair in masterFEIdSlaveSegments)
                {
                    uint masterFEId = pair.Key;
                    IList<OpenTK.Vector3d[]> triSegPtss = pair.Value;
                    for (int iSeg = 0; iSeg < triSegPtss.Count; iSeg++)
                    {
                        OpenTK.Vector3d[] triSegPts = triSegPtss[iSeg];
                        double[][] segVertexLs = new double[3][];
                        for (int iVertex = 0; iVertex < 3; iVertex++)
                        {
                            OpenTK.Vector3d pt = triSegPts[iVertex];
                            double[] coord = new double[3] { pt.X, pt.Y, pt.Z };
                            segVertexLs[iVertex] = triFE.Coord2L(coord);
                            segVertexLs[iVertex] = ModifyL(segVertexLs[iVertex]);
                        }
                        double segA = CadUtils3D.TriArea(triSegPts[0], triSegPts[1], triSegPts[2]);
                        if (segA < Constants.PrecisionLowerLimit)
                        {
                            // セグメントが小さすぎる
                            continue;
                        }
                        for (int ipPt = 0; ipPt < ip.PointCount; ipPt++)
                        {
                            double[] segL = ip.Ls[ipPt];
                            double weight = ip.Weights[ipPt];
                            double detJ = 2.0 * segA;
                            double detJWeight = (1.0 / 2.0) * weight * detJ;
                            double[] L = new double[3];
                            for (int iVertex = 0; iVertex < 3; iVertex++)
                            {
                                for (int iL = 0; iL < 3; iL++)
                                {
                                    L[iL] += segVertexLs[iVertex][iL] * segL[iVertex];
                                }
                            }
                            double[] N = triFE.CalcN(L);
                            double[] lN = lTriFE.CalcN(L);
                            // 現在の位置
                            double[] curCoord = new double[uDof];
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                for (int iDof = 0; iDof < uDof; iDof++)
                                {
                                    curCoord[iDof] += curNodeCoord[iNode][iDof] * N[iNode];
                                }
                            }
                            /*
                            // 連続な近似法線ベクトルを計算する
                            double[] normal = new double[uDof];
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                int coId = triFE.NodeCoordIds[iNode];
                                double[] nodeNormal = co2Normal[coId];
                                for (int iDof = 0; iDof < uDof; iDof++)
                                {
                                    normal[iDof] += nodeNormal[iDof] * N[iNode];
                                }
                            }
                            normal = IvyFEM.Lapack.Utils.NormalizeDoubleVector(normal);
                            */
                            // 連続な法線ベクトルの三角形の重心の値で代表する
                            double[] normal = new double[3] { slaveNormal.X, slaveNormal.Y, slaveNormal.Z };

                            OpenTK.Vector3d normalVec = new OpenTK.Vector3d(normal[0], normal[1], normal[2]);
                            OpenTK.Vector3d tanVec1 = CadUtils3D.GetVerticalUnitVector(normalVec);
                            OpenTK.Vector3d tanVec2 = OpenTK.Vector3d.Cross(normalVec, tanVec1);
                            double[] tan1 = new double[3] { tanVec1.X, tanVec1.Y, tanVec1.Z };
                            double[] tan2 = new double[3] { tanVec2.X, tanVec2.Y, tanVec2.Z };

                            OpenTK.Vector3d masterPt;
                            {
                                bool isIntersect = GetMasterTriangleFEPoint2(
                                    masterFEId,
                                    new OpenTK.Vector3d(curCoord[0], curCoord[1], curCoord[2]),
                                    new OpenTK.Vector3d(normal[0], normal[1], normal[2]),
                                    uQuantityId, uDof, cQuantityId,
                                    out masterPt);
                                if (!isIntersect)
                                {
                                    continue;
                                }
                            }
                            double[] masterCoord = new double[3] { masterPt.X, masterPt.Y, masterPt.Z };

                            TriangleFE masterTriFE = World.GetTriangleFE(uQuantityId, masterFEId);
                            System.Diagnostics.Debug.Assert(masterTriFE.NodeCount == elemNodeCnt);
                            double[] masterL = masterTriFE.Coord2L(masterCoord);
                            double[] masterN = masterTriFE.CalcN(masterL);
                            int[] masterNodes = new int[elemNodeCnt];
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                int coId = masterTriFE.NodeCoordIds[iNode];
                                int nodeId = World.Coord2Node(uQuantityId, coId);
                                masterNodes[iNode] = nodeId;
                            }
                            // 現在の位置
                            double[] masterCurCoord = new double[uDof];
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                int coId = masterTriFE.NodeCoordIds[iNode];
                                double[] coord = World.GetCoord(uQuantityId, coId);
                                int iNodeId = masterNodes[iNode];
                                if (iNodeId == -1)
                                {
                                    for (int iDof = 0; iDof < uDof; iDof++)
                                    {
                                        masterCurCoord[iDof] += coord[iDof] * masterN[iNode];
                                    }
                                }
                                else
                                {
                                    for (int iDof = 0; iDof < uDof; iDof++)
                                    {
                                        masterCurCoord[iDof] +=
                                            (coord[iDof] + U[iNodeId * uDof + iDof]) * masterN[iNode];
                                    }
                                }
                            }

                            // ギャップの計算
                            double gap = 0;
                            for (int iDof = 0; iDof < uDof; iDof++)
                            {
                                gap += -normal[iDof] * (curCoord[iDof] - masterCurCoord[iDof]);
                            }

                            // ラグランジュの未定乗数
                            double[] l = new double[cDof];
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                int iNodeId = lNodes[iNode];
                                if (iNodeId == -1)
                                {
                                    continue;
                                }
                                for (int iDof = 0; iDof < cDof; iDof++)
                                {
                                    l[iDof] += U[offset + iNodeId * cDof + iDof] * lN[iNode];
                                }
                            }
                            // Karush-Kuhn-Tucker条件
                            // NOTE: l[0]: normal、l[1]: tan1 l[2]: tan2
                            double tolerance = IvyFEM.Linear.Constants.ConvRatioTolerance;
                            if (l[0] <= tolerance &&
                                Math.Abs(l[1]) <= tolerance &&
                                Math.Abs(l[2]) <= tolerance &&
                                gap >= -tolerance)
                            {
                                // 拘束しない
                                continue;
                            }

                            ////////////////////////////////////////
                            // これ以降、条件を付加する処理
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                int iNodeId = lNodes[iNode];
                                if (iNodeId == -1)
                                {
                                    continue;
                                }
                                lConstraintNodeIds[iNodeId] = true;
                            }

                            // Slave
                            for (int row = 0; row < elemNodeCnt; row++)
                            {
                                int rowNodeId = nodes[row];
                                if (rowNodeId == -1)
                                {
                                    continue;
                                }
                                for (int col = 0; col < elemNodeCnt; col++)
                                {
                                    int colNodeId = lNodes[col];
                                    if (colNodeId == -1)
                                    {
                                        continue;
                                    }

                                    double[,] kul = new double[uDof, cDof];
                                    double[,] klu = new double[cDof, uDof];
                                    for (int rowDof = 0; rowDof < uDof; rowDof++)
                                    {
                                        kul[rowDof, 0] +=
                                            detJWeight * normal[rowDof] * N[row] * lN[col];
                                        klu[0, rowDof] +=
                                            detJWeight * normal[rowDof] * N[row] * lN[col];
                                        kul[rowDof, 1] +=
                                            detJWeight * tan1[rowDof] * N[row] * lN[col];
                                        klu[1, rowDof] +=
                                            detJWeight * tan1[rowDof] * N[row] * lN[col];
                                        kul[rowDof, 2] +=
                                            detJWeight * tan2[rowDof] * N[row] * lN[col];
                                        klu[2, rowDof] +=
                                            detJWeight * tan2[rowDof] * N[row] * lN[col];
                                    }

                                    for (int rowDof = 0; rowDof < uDof; rowDof++)
                                    {
                                        for (int colDof = 0; colDof < cDof; colDof++)
                                        {
                                            A[rowNodeId * uDof + rowDof, offset + colNodeId * cDof + colDof] +=
                                                kul[rowDof, colDof];
                                            A[offset + colNodeId * cDof + colDof, rowNodeId * uDof + rowDof] +=
                                                klu[colDof, rowDof];
                                            B[rowNodeId * uDof + rowDof] +=
                                                kul[rowDof, colDof] * U[offset + colNodeId * cDof + colDof];
                                            B[offset + colNodeId * cDof + colDof] +=
                                                klu[colDof, rowDof] * U[rowNodeId * uDof + rowDof];
                                        }
                                    }
                                }
                            }

                            // Master
                            for (int row = 0; row < elemNodeCnt; row++)
                            {
                                int rowNodeId = masterNodes[row];
                                if (rowNodeId == -1)
                                {
                                    continue;
                                }
                                for (int col = 0; col < elemNodeCnt; col++)
                                {
                                    int colNodeId = lNodes[col];
                                    if (colNodeId == -1)
                                    {
                                        continue;
                                    }

                                    double[,] kul = new double[uDof, cDof];
                                    double[,] klu = new double[cDof, uDof];
                                    for (int rowDof = 0; rowDof < uDof; rowDof++)
                                    {
                                        kul[rowDof, 0] +=
                                            -detJWeight * normal[rowDof] * masterN[row] * lN[col];
                                        klu[0, rowDof] +=
                                            -detJWeight * normal[rowDof] * masterN[row] * lN[col];
                                        kul[rowDof, 1] +=
                                            -detJWeight * tan1[rowDof] * masterN[row] * lN[col];
                                        klu[1, rowDof] +=
                                            -detJWeight * tan1[rowDof] * masterN[row] * lN[col];
                                        kul[rowDof, 2] +=
                                            -detJWeight * tan2[rowDof] * masterN[row] * lN[col];
                                        klu[2, rowDof] +=
                                            -detJWeight * tan2[rowDof] * masterN[row] * lN[col];
                                    }

                                    for (int rowDof = 0; rowDof < uDof; rowDof++)
                                    {
                                        for (int colDof = 0; colDof < cDof; colDof++)
                                        {
                                            A[rowNodeId * uDof + rowDof, offset + colNodeId * cDof + colDof] +=
                                                kul[rowDof, colDof];
                                            A[offset + colNodeId * cDof + colDof, rowNodeId * uDof + rowDof] +=
                                                klu[colDof, rowDof];
                                            B[rowNodeId * uDof + rowDof] +=
                                                kul[rowDof, colDof] * U[offset + colNodeId * cDof + colDof];
                                            B[offset + colNodeId * cDof + colDof] +=
                                                klu[colDof, rowDof] * U[rowNodeId * uDof + rowDof];
                                        }
                                    }
                                }
                            }

                            // Slave
                            double[,] qu = new double[elemNodeCnt, uDof];
                            double[,] ql = new double[elemNodeCnt, cDof];
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                for (int iDof = 0; iDof < uDof; iDof++)
                                {
                                    qu[iNode, iDof] +=
                                        detJWeight * (
                                        l[0] * normal[iDof] + l[1] * tan1[iDof] + l[2] * tan2[iDof]) * N[iNode];
                                }
                            }
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                for (int iDof = 0; iDof < uDof; iDof++)
                                {
                                    ql[iNode, 0] +=
                                        detJWeight * lN[iNode] * normal[iDof] * curCoord[iDof];
                                    ql[iNode, 1] +=
                                        detJWeight * lN[iNode] * tan1[iDof] * curCoord[iDof];
                                    ql[iNode, 2] +=
                                        detJWeight * lN[iNode] * tan2[iDof] * curCoord[iDof];
                                }
                            }

                            for (int row = 0; row < elemNodeCnt; row++)
                            {
                                int rowNodeId = nodes[row];
                                if (rowNodeId == -1)
                                {
                                    continue;
                                }
                                for (int rowDof = 0; rowDof < uDof; rowDof++)
                                {
                                    B[rowNodeId * uDof + rowDof] += -qu[row, rowDof];
                                }
                            }
                            for (int row = 0; row < elemNodeCnt; row++)
                            {
                                int rowNodeId = lNodes[row];
                                if (rowNodeId == -1)
                                {
                                    continue;
                                }
                                for (int rowDof = 0; rowDof < cDof; rowDof++)
                                {
                                    B[offset + rowNodeId * cDof + rowDof] += -ql[row, rowDof];
                                }
                            }

                            // Master
                            double[,] masterQu = new double[elemNodeCnt, uDof];
                            double[,] masterQl = new double[elemNodeCnt, cDof];
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                for (int iDof = 0; iDof < uDof; iDof++)
                                {
                                    masterQu[iNode, iDof] +=
                                        -detJWeight * (
                                        l[0] * normal[iDof] + l[1] * tan1[iDof] + l[2] * tan2[iDof]) * masterN[iNode];
                                }
                            }
                            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                            {
                                for (int iDof = 0; iDof < uDof; iDof++)
                                {
                                    masterQl[iNode, 0] +=
                                        -detJWeight * lN[iNode] * normal[iDof] * masterCurCoord[iDof];
                                    masterQl[iNode, 1] +=
                                        -detJWeight * lN[iNode] * tan1[iDof] * masterCurCoord[iDof];
                                    masterQl[iNode, 2] +=
                                        -detJWeight * lN[iNode] * tan2[iDof] * masterCurCoord[iDof];
                                }
                            }

                            for (int row = 0; row < elemNodeCnt; row++)
                            {
                                int rowNodeId = masterNodes[row];
                                if (rowNodeId == -1)
                                {
                                    continue;
                                }
                                for (int rowDof = 0; rowDof < uDof; rowDof++)
                                {
                                    B[rowNodeId * uDof + rowDof] += -masterQu[row, rowDof];
                                }
                            }
                            for (int row = 0; row < elemNodeCnt; row++)
                            {
                                int rowNodeId = lNodes[row];
                                if (rowNodeId == -1)
                                {
                                    continue;
                                }
                                for (int rowDof = 0; rowDof < cDof; rowDof++)
                                {
                                    B[offset + rowNodeId * cDof + rowDof] += -masterQl[row, rowDof];
                                }
                            }
                        }
                    }
                }
            }

            // 条件をセットしなかった節点
            for (int iNodeId = 0; iNodeId < cNodeCnt; iNodeId++)
            {
                if (lConstraintNodeIds[iNodeId])
                {
                    continue;
                }
                for (int iDof = 0; iDof < cDof; iDof++)
                {
                    A[offset + iNodeId * cDof + iDof, offset + iNodeId * cDof + iDof] = 1.0;
                    B[offset + iNodeId * cDof + iDof] = 0;
                }
            }

            // 後片付け
            World.ClearFEDisplacements(uQuantityId);
            World.ClearFEDisplacements(cQuantityId);
        }
    }
}
