﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    public class EdgeFieldDrawer : IFieldDrawer
    {
        public uint LineWidth { get; set; } = 1;
        private VertexArray VertexArray = new VertexArray();
        private IList<LineFE> LineFEs = new List<LineFE>();
        private uint LineCount => (uint)LineFEs.Count;
        private uint LinePtCount = 0;
        private uint ValueId = 0;
        private FieldDerivativeType ValueDt = FieldDerivativeType.Value;
        private bool IsntDisplacementValue = false;
        private bool IsDrawInnerEdge = true;
        public RotMode SutableRotMode { get; private set; } = RotMode.RotModeNotSet;
        public bool IsAntiAliasing { get; set; } = false;

        public EdgeFieldDrawer()
        {

        }

        public EdgeFieldDrawer(uint valueId, FieldDerivativeType valueDt,
            bool isntDisplacementValue,
            bool isDrawInnerEdge,
            FEWorld world)
        {
            Set(valueId, valueDt, isntDisplacementValue, isDrawInnerEdge, world);
        }

        private void Set(uint valueId, FieldDerivativeType valueDt,
            bool isntDisplacementValue,
            bool isDrawInnerEdge,
            FEWorld world)
        {
            var mesh = world.Mesh;

            if (!world.IsFieldValueId(valueId))
            {
                throw new ArgumentException();
                //return;
            }

            ValueId = valueId;
            ValueDt = valueDt;
            IsntDisplacementValue = isntDisplacementValue;

            var fv = world.GetFieldValue(ValueId);

            // 線要素を生成
            uint quantityId = fv.QuantityId;
            if (isDrawInnerEdge)
            {
                // 内部の全ての辺を描画
                LineFEs = world.MakeLineElementsForDraw(quantityId);
            }
            else
            {
                // 境界の辺だけ描画
                LineFEs = new List<LineFE>();
                
                IList<uint> meshIds = mesh.GetIds();
                foreach (uint meshId in meshIds)
                {
                    uint elemCnt; 
                    MeshType meshType;
                    int loc;
                    uint cadId;
                    mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                    if (meshType != MeshType.Bar)
                    {
                        continue;
                    }
                    uint eId = cadId;
                    IList<int> allCoIds = world.GetCoordIdsFromCadId(quantityId, eId, CadElementType.Edge);

                    for (int i = 0; i < (allCoIds.Count - 1); i++)
                    {
                        int workFEOrder = 1;
                        FiniteElementType workFEType = FiniteElementType.ScalarLagrange;
                        LineFE lineFE = new LineFE(workFEOrder, workFEType);
                        lineFE.World = world;
                        lineFE.QuantityId = (int)quantityId;
                        {
                            int[] coIds = { allCoIds[i], allCoIds[i + 1] };
                            lineFE.SetVertexCoordIds(coIds);
                            lineFE.SetNodeCoordIds(coIds);
                        }
                        LineFEs.Add(lineFE);
                    }

                }
            }

            int feOrder;
            {
                LineFE lineFE = LineFEs[0]; // 先頭の要素
                feOrder = lineFE.Order;
                LinePtCount = lineFE.NodeCount;
            }

            uint ptCnt = LineCount * LinePtCount;
            uint dim = world.Dimension;

            uint drawDim;
            if (!IsntDisplacementValue
                && dim == 2
                && (fv.Type == FieldValueType.Scalar || fv.Type == FieldValueType.ZScalar))
            {
                drawDim = 3;
            }
            else
            {
                drawDim = dim;
            }
            VertexArray.SetSize(ptCnt, drawDim);

            if (drawDim == 2)
            {
                SutableRotMode = RotMode.RotMode2D;
            }
            else if (drawDim == 3)
            {
                if (dim == 2)
                {
                    SutableRotMode = RotMode.RotMode2DH;
                }
                else if (dim == 3)
                {
                    SutableRotMode = RotMode.RotMode3D;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            Update(world);
        }

        public void Update(FEWorld world)
        {
            FieldValue fv = world.GetFieldValue(ValueId);
            uint quantityId = fv.QuantityId;
            uint dim = world.Dimension;
            uint ptCnt = LineCount * LinePtCount;
            uint lineCnt = LineCount;
            uint drawDim = VertexArray.Dimension;

            if (IsntDisplacementValue)
            {
                for (int iEdge = 0; iEdge < lineCnt; iEdge++)
                {
                    LineFE lineFE = LineFEs[iEdge];
                    for (int iPt = 0; iPt < LinePtCount; iPt++)
                    {
                        int coId = lineFE.NodeCoordIds[iPt];
                        double[] co = world.GetCoord(quantityId, coId);
                        for (int idim = 0; idim < drawDim; idim++)
                        {
                            VertexArray.VertexCoords[(iEdge * LinePtCount + iPt) * drawDim + idim] = co[idim];
                        }
                    }
                }
            }
            else
            {
                // 変位を伴う場合

                if (dim == 2 && drawDim == 3)
                {
                    for (int iEdge = 0; iEdge < LineCount; iEdge++)
                    {
                        LineFE lineFE = LineFEs[iEdge];
                        System.Diagnostics.Debug.Assert(lineFE.NodeCoordIds.Length == LinePtCount);
                        for (int iPt = 0; iPt < LinePtCount; iPt++)
                        {
                            int coId = lineFE.NodeCoordIds[iPt];
                            double[] coord = world.GetCoord(quantityId, coId);
                            FieldDerivativeType dt = ValueDt;
                            double value = fv.GetShowValue(coId, 0, dt);
                            VertexArray.VertexCoords[(iEdge * LinePtCount + iPt) * drawDim + 0] = coord[0];
                            VertexArray.VertexCoords[(iEdge * LinePtCount + iPt) * drawDim + 1] = coord[1];
                            VertexArray.VertexCoords[(iEdge * LinePtCount + iPt) * drawDim + 2] = value;
                        }
                    }
                }
                else
                {
                    for (int iEdge = 0; iEdge < lineCnt; iEdge++)
                    {
                        LineFE lineFE = LineFEs[iEdge];
                        System.Diagnostics.Debug.Assert(lineFE.NodeCoordIds.Length == LinePtCount);
                        for (int iPt = 0; iPt < LinePtCount; iPt++)
                        {
                            int coId = lineFE.NodeCoordIds[iPt];
                            double[] coord = world.GetCoord(quantityId, coId);
                            FieldDerivativeType dt = ValueDt;
                            for (int iDim = 0; iDim < drawDim; iDim++)
                            {
                                double value = fv.GetShowValue(coId, iDim, dt);
                                VertexArray.VertexCoords[(iEdge * LinePtCount + iPt) * drawDim + iDim] =
                                    coord[iDim] + value;
                            }
                        }
                    }
                }
            }
        }

        public void Draw()
        {
            if (LineCount == 0)
            {
                return;
            }

            bool isTexture = GL.IsEnabled(EnableCap.Texture2D);
            bool isLighting = GL.IsEnabled(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);

            GL.Color3(0.0, 0.0, 0.0);
            GL.LineWidth(LineWidth);

            uint drawDim = VertexArray.Dimension;
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer((int)drawDim, VertexPointerType.Double, 0, VertexArray.VertexCoords);
            if (drawDim == 2)
            {
                GL.Translate(0, 0, +0.01);
            }

            if (LinePtCount == 2) // 1次要素
            {
                GL.DrawArrays(PrimitiveType.Lines, 0, (int)(LineCount * LinePtCount));
            }
            else if (LinePtCount == 3) // 2次要素
            {
                GL.Begin(PrimitiveType.Lines);
                for (int iEdge = 0; iEdge < LineCount; iEdge++)
                {
                    GL.ArrayElement((int)(iEdge * LinePtCount + 0));
                    GL.ArrayElement((int)(iEdge * LinePtCount + 2));
                    GL.ArrayElement((int)(iEdge * LinePtCount + 2));
                    GL.ArrayElement((int)(iEdge * LinePtCount + 1));
                }
                GL.End();
            }

            if (drawDim == 2)
            {
                GL.Translate(0, 0, -0.01);
            }

            GL.DisableClientState(ArrayCap.VertexArray);
            if (isTexture)
            {
                GL.Enable(EnableCap.Texture2D);
            }
            if (isLighting)
            {
                GL.Enable(EnableCap.Lighting);
            }
        }

        public void DrawSelection(uint idraw)
        {
            throw new NotImplementedException();
        }

        public void AddSelected(int[] selectFlag)
        {
            throw new NotImplementedException();
        }

        public void ClearSelected()
        {
            throw new NotImplementedException();
        }

        public BoundingBox3D GetBoundingBox(OpenTK.Matrix3d rot)
        {
            return VertexArray.GetBoundingBox(rot);
        }

    }
}
