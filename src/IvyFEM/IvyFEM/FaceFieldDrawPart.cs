﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    public class FaceFieldDrawPart
    {
        public byte[] Mask { get; set; } = null;
        public bool IsSelected { get; set; } = false;
        public uint MeshId { get; set; } = 0; 
        public ElementType Type { get; private set; } = ElementType.NotSet;
        public int Layer { get; set; } = 0;
        public double[] Color { get; set; } = new double[3] { 0.8, 0.8, 0.8 };
        public uint ElemCount { get; set; } = 0;
        public uint ElemPtCount { get; set; } = 0;
        public uint[] Indexs { get; set; } = null;
        public double[] Colors { get; set; } = null;

        public uint Dimension
        {
            get
            {
                if (Type == ElementType.Point)
                {
                    return 0;
                }
                else if (Type == ElementType.Line)
                {
                    return 1;
                }
                else if (Type == ElementType.Tri)
                {
                    return 2;
                }
                else if (Type == ElementType.Tet)
                {
                    return 3;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                return 0;
            }
        }

        public FaceFieldDrawPart()
        {

        }

        public FaceFieldDrawPart(FaceFieldDrawPart src)
        {
            IsSelected = src.IsSelected;
            MeshId = src.MeshId;
            Type = src.Type;
            Layer = src.Layer;
            src.Color.CopyTo(Color, 0);
            ElemCount = src.ElemCount;
            ElemPtCount = src.ElemPtCount;
            Indexs = null;
            if (src.Indexs != null)
            {
                Indexs = new uint[src.Indexs.Length];
                src.Indexs.CopyTo(Indexs, 0);
            }
            Colors = null;
            if (src.Colors != null)
            {
                Colors = new double[src.Colors.Length];
                src.Colors.CopyTo(Colors, 0);
            }
        }

        public FaceFieldDrawPart(uint meshId, FEWorld world, uint valueId)
        {
            var mesh = world.Mesh;
            if (!mesh.IsId(meshId))
            {
                return;
            }
            MeshId = meshId;

            uint cadId;
            int layer;
            uint elemCount;
            MeshType meshType;
            int loc;
            mesh.GetInfo(MeshId, out cadId, out layer);
            mesh.GetMeshInfo(MeshId, out elemCount, out meshType, out loc, out cadId);
            Layer = layer;
            ElemCount = elemCount;

            if (meshType == MeshType.Vertex)
            {
                Type = ElementType.Point;
                Color = new double[3] { 0.0, 0.0, 0.0 };
            }
            else if (meshType == MeshType.Bar)
            {
                Type = ElementType.Line;
                Color = new double[3] { 0.0, 0.0, 0.0 };
                SetLine(world, valueId);
            }
            else if (meshType == MeshType.Tri)
            {
                Type = ElementType.Tri;
                SetTri(world, valueId);
            }
            else if (meshType == MeshType.Tet)
            {
                Type = ElementType.Tet;
                SetTet(world, valueId);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void SetLine(FEWorld world, uint valueId)
        {
            System.Diagnostics.Debug.Assert(Type == ElementType.Line);
            if (Type != ElementType.Line)
            {
                return;
            }

            FieldValue fv = world.GetFieldValue(valueId);
            uint quantityId = fv.QuantityId;
            int feOrder;
            {
                uint feId = world.GetLineFEIdFromMesh(quantityId, MeshId, 0); // 先頭の要素
                System.Diagnostics.Debug.Assert(feId != 0);
                LineFE lineFE = world.GetLineFE(quantityId, feId);
                feOrder = lineFE.Order;
                ElemPtCount = lineFE.NodeCount;
            }

            Indexs = new uint[ElemPtCount * ElemCount];
            for (int iEdge = 0; iEdge < ElemCount; iEdge++)
            {
                uint feId = world.GetLineFEIdFromMesh(quantityId, MeshId, (uint)iEdge);
                System.Diagnostics.Debug.Assert(feId != 0);
                LineFE lineFE = world.GetLineFE(quantityId, feId);
                for (int iPt = 0; iPt < ElemPtCount; iPt++)
                {
                    Indexs[iEdge * ElemPtCount + iPt] = (uint)lineFE.NodeCoordIds[iPt];
                }
            }
        }

        private void SetTri(FEWorld world, uint valueId)
        {
            System.Diagnostics.Debug.Assert(Type == ElementType.Tri);
            if (Type != ElementType.Tri)
            {
                return;
            }

            FieldValue fv = world.GetFieldValue(valueId);
            uint quantityId = fv.QuantityId;
            int feOrder;
            {
                uint feId = world.GetTriangleFEIdFromMesh(quantityId, MeshId, 0); // 先頭の要素
                System.Diagnostics.Debug.Assert(feId != 0);
                TriangleFE triFE = world.GetTriangleFE(quantityId, feId);
                feOrder = triFE.Order;
                ElemPtCount = triFE.NodeCount;
            }

            Indexs = new uint[ElemPtCount * ElemCount];
            for (int iTri = 0; iTri < ElemCount; iTri++)
            {
                uint feId = world.GetTriangleFEIdFromMesh(quantityId, MeshId, (uint)iTri);
                System.Diagnostics.Debug.Assert(feId != 0);
                TriangleFE triFE = world.GetTriangleFE(quantityId, feId);
                for (int iPt = 0; iPt < ElemPtCount; iPt++)
                {
                    Indexs[iTri * ElemPtCount + iPt] = (uint)triFE.NodeCoordIds[iPt];
                }
            }
        }

        private void SetTet(FEWorld world, uint valueId)
        {
            System.Diagnostics.Debug.Assert(Type == ElementType.Tet);
            if (Type != ElementType.Tet)
            {
                return;
            }

            FieldValue fv = world.GetFieldValue(valueId);
            uint quantityId = fv.QuantityId;
            int feOrder;
            {
                uint feId = world.GetTetrahedronFEIdFromMesh(quantityId, MeshId, 0); // 先頭の要素
                System.Diagnostics.Debug.Assert(feId != 0);
                TetrahedronFE tetFE = world.GetTetrahedronFE(quantityId, feId);
                feOrder = tetFE.Order;
                ElemPtCount = tetFE.NodeCount;
            }

            Indexs = new uint[ElemPtCount * ElemCount];
            for (int iTet = 0; iTet < ElemCount; iTet++)
            {
                uint feId = world.GetTetrahedronFEIdFromMesh(quantityId, MeshId, (uint)iTet);
                System.Diagnostics.Debug.Assert(feId != 0);
                TetrahedronFE tetFE = world.GetTetrahedronFE(quantityId, feId);
                for (int iPt = 0; iPt < ElemPtCount; iPt++)
                {
                    Indexs[iTet * ElemPtCount + iPt] = (uint)tetFE.NodeCoordIds[iPt];
                }
            }
        }

        public void SetColors(uint bubbleValueId, FieldDerivativeType dt, FEWorld world, IColorMap colorMap)
        {
            FieldValue fv = world.GetFieldValue(bubbleValueId);
            System.Diagnostics.Debug.Assert(fv.IsBubble == true);
            uint quantityId = fv.QuantityId;
            var mesh = world.Mesh;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);

            if (Type == ElementType.Tri)
            {
                Colors = new double[ElemCount * 3];
                for (int iTri = 0; iTri < ElemCount; iTri++)
                {
                    // Bubble
                    uint feId = world.GetTriangleFEIdFromMesh(quantityId, MeshId, (uint)iTri);
                    System.Diagnostics.Debug.Assert(feId != 0);
                    double value = fv.GetShowValue((int)(feId - 1), 0, dt);
                    var color = colorMap.GetColor(value);
                    for (int iColor = 0; iColor < 3; iColor++)
                    {
                        Colors[iTri * 3 + iColor] = color[iColor];
                    }
                }
            }
            else if (Type == ElementType.Tet)
            {
                Colors = new double[ElemCount * 3];
                for (int iTet = 0; iTet < ElemCount; iTet++)
                {
                    // Bubble
                    uint feId = world.GetTetrahedronFEIdFromMesh(quantityId, MeshId, (uint)iTet);
                    System.Diagnostics.Debug.Assert(feId != 0);
                    double value = fv.GetShowValue((int)(feId - 1), 0, dt);
                    var color = colorMap.GetColor(value);
                    for (int iColor = 0; iColor < 3; iColor++)
                    {
                        Colors[iTet * 3 + iColor] = color[iColor];
                    }
                }
            }
        }

        public void ClearColors()
        {
            Colors = null;
        }

        public uint[] GetVertexs(uint iElem)
        {
            // 頂点だけを対象とする
            int vertexCnt = 0;
            if (Type == ElementType.Tri)
            {
                vertexCnt = 3;
            }
            uint[] vertexs = new uint[vertexCnt]; 
            for (int iPt = 0; iPt < vertexCnt; iPt++)
            {
                vertexs[iPt] = Indexs[iElem * ElemPtCount + iPt];
            }
            return vertexs;
        }

        public void DrawElements()
        {
            if (Colors == null)
            {
                //GL.Color3(Color[0], Color[1], Color[2]);
                if (Type == ElementType.Line)
                {
                    if (ElemPtCount == 2) // 1次要素
                    {
                        GL.DrawElements(PrimitiveType.Lines, (int)(ElemCount * ElemPtCount), DrawElementsType.UnsignedInt, Indexs);
                    }
                    else if (ElemPtCount == 3) // 2次要素
                    {
                        GL.Begin(PrimitiveType.Lines);
                        for (int iEdge = 0; iEdge < ElemCount; iEdge++)
                        {
                            GL.ArrayElement((int)Indexs[iEdge * ElemPtCount + 0]);
                            GL.ArrayElement((int)Indexs[iEdge * ElemPtCount + 2]);
                            GL.ArrayElement((int)Indexs[iEdge * ElemPtCount + 2]);
                            GL.ArrayElement((int)Indexs[iEdge * ElemPtCount + 1]);
                        }
                        GL.End();
                    }
                }
                else if (Type == ElementType.Tri) 
                {
                    if (Mask != null)
                    {
                        //----------------------
                        GL.Enable(EnableCap.PolygonStipple);
                        GL.PolygonStipple(Mask);
                        //----------------------
                    }
                    if (ElemPtCount == 3) // 1次要素
                    {
                        GL.DrawElements(PrimitiveType.Triangles, (int)(ElemCount * ElemPtCount), DrawElementsType.UnsignedInt, Indexs);
                    }
                    else if (ElemPtCount == 6) // 2次要素
                    {
                        GL.Begin(PrimitiveType.Triangles);
                        for (int iTri = 0; iTri < ElemCount; iTri++)
                        {
                            // 4つの三角形
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 0]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 3]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 5]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 3]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 4]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 5]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 3]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 1]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 4]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 5]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 4]);
                            GL.ArrayElement((int)Indexs[iTri * ElemPtCount + 2]);
                        }
                        GL.End();
                    }
                    if (Mask != null)
                    {
                        //----------------------
                        GL.Disable(EnableCap.PolygonStipple);
                        //----------------------
                    }
                }
                else if (Type == ElementType.Tet)
                {
                    if (Mask != null)
                    {
                        //----------------------
                        GL.Enable(EnableCap.PolygonStipple);
                        GL.PolygonStipple(Mask);
                        //----------------------
                    }
                    //if (ElemPtCount == 4) // 1次要素
                    {
                        GL.Begin(PrimitiveType.Triangles);
                        for (int iTet = 0; iTet < ElemCount; iTet++)
                        {
                            // 4つの面(三角形)
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 1]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 2]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 3]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 2]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 0]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 3]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 0]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 1]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 3]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 2]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 1]);
                            GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 0]);
                        }
                        GL.End();
                    }
                    if (Mask != null)
                    {
                        //----------------------
                        GL.Disable(EnableCap.PolygonStipple);
                        //----------------------
                    }
                }
                return;
            }

            if (Type == ElementType.Tri)
            {
                // 要素全体を塗り潰すので要素の次数に関係なく1つの三角形を描画
                GL.Begin(PrimitiveType.Triangles);
                for (int iTri = 0; iTri < ElemCount; iTri++)
                {
                    GL.Color3(
                        Colors[iTri * 3], Colors[iTri * 3 + 1], Colors[iTri * 3 + 2]);
                    // 先頭の3点は頂点
                    for (int iPt = 0; iPt < 3; iPt++)
                    {
                        GL.ArrayElement((int)Indexs[iTri * ElemPtCount + iPt]);
                    }
                }
                GL.End();
            }
            else if (Type == ElementType.Tet)
            {
                // 要素全体を塗り潰すので要素の次数に関係なく4つの三角形を描画
                GL.Begin(PrimitiveType.Triangles);
                for (int iTet = 0; iTet < ElemCount; iTet++)
                {
                    GL.Color3(
                        Colors[iTet * 3], Colors[iTet * 3 + 1], Colors[iTet * 3 + 2]);
                    {
                        // 4つの面(三角形)
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 1]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 2]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 3]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 2]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 0]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 3]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 0]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 1]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 3]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 2]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 1]);
                        GL.ArrayElement((int)Indexs[iTet * ElemPtCount + 0]);
                    }
                }
                GL.End();
            }
        }
    }
}
