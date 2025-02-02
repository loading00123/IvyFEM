﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public partial class Mesher2D : IMesher
    {
        public uint Dimension => 2;
        public Cad2D Cad { get; private set; } = null;
        public double MinTriArea { get; set; } = CadUtils2D.DefMinTriArea;
        private IList<CadIdELen> MeshingEdgeCadIdELens = new List<CadIdELen>();
        private IList<CadIdELen> MeshingLoopCadIdELens = new List<CadIdELen>();
        private uint MeshingMode = 0; // 0: for cad 1: for mesh

        private IList<MeshTypeLoc> TypeLocs = new List<MeshTypeLoc>();

        private IList<MeshVertex2D> Vertexs = new List<MeshVertex2D>();
        private IList<MeshBarArray2D> BarArrays = new List<MeshBarArray2D>();
        private IList<MeshTriArray2D> TriArrays = new List<MeshTriArray2D>();

        private IList<OpenTK.Vector2d> Vecs = new List<OpenTK.Vector2d>();

        public Mesher2D()
        {
            MeshingMode = 1;
        }

        public Mesher2D(Cad2D cad)
        {
            Cad = cad;
            MinTriArea = Cad.MinTriArea;
            MeshingMode = 0;
            double eLen = 1.0;
            IList<uint> lIds = cad.GetElementIds(CadElementType.Loop);
            for (uint i = 0; i < lIds.Count; i++)
            {
                var cadIdELen = new CadIdELen { CadId = lIds[(int)i], ELen = eLen };
                MeshingLoopCadIdELens.Add(cadIdELen);
            }

            MakeMesh(Cad);
        }

        public Mesher2D(Cad2D cad, double eLen)
        {
            Cad = cad;
            MinTriArea = Cad.MinTriArea;
            MeshingMode = 1;

            IList<uint> lIds = cad.GetElementIds(CadElementType.Loop);
            for (int i = 0; i < lIds.Count; i++)
            {
                var cadIdELen = new CadIdELen { CadId = lIds[(int)i], ELen = eLen };
                MeshingLoopCadIdELens.Add(cadIdELen);
            }

            MakeMesh(Cad);
        }

        public Mesher2D(
            Cad3DToXY cad,
            IList<OpenTK.Vector2d> initialMeshVec2Ds,
            IList<MeshVertex2D> initialMeshVertex2Ds,
            IList<MeshBarArray2D> initialMeshBarArray2Ds)
        {
            Cad = cad;
            MinTriArea = Cad.MinTriArea;
            MeshingMode = 0;
            double eLen = 1.0;
            IList<uint> lIds = cad.GetElementIds(CadElementType.Loop);
            for (uint i = 0; i < lIds.Count; i++)
            {
                var cadIdELen = new CadIdELen { CadId = lIds[(int)i], ELen = eLen };
                MeshingLoopCadIdELens.Add(cadIdELen);
            }

            MakeMeshFrom3D(cad, initialMeshVec2Ds, initialMeshVertex2Ds, initialMeshBarArray2Ds);
        }

        public Mesher2D(Cad3DToXY cad, double eLen,
            IList<OpenTK.Vector2d> initialMeshVec2Ds,
            IList<MeshVertex2D> initialMeshVertex2Ds,
            IList<MeshBarArray2D> initialMeshBarArray2Ds)
        {
            Cad = cad;
            MinTriArea = Cad.MinTriArea;
            MeshingMode = 1;

            IList<uint> lIds = cad.GetElementIds(CadElementType.Loop);
            for (int i = 0; i < lIds.Count; i++)
            {
                var cadIdELen = new CadIdELen { CadId = lIds[(int)i], ELen = eLen };
                MeshingLoopCadIdELens.Add(cadIdELen);
            }

            MakeMeshFrom3D(cad, initialMeshVec2Ds, initialMeshVertex2Ds, initialMeshBarArray2Ds);
        }

        public Mesher2D(Mesher2D src)
        {
            Clear();
            Cad = src.Cad;
            MinTriArea = src.MinTriArea;
            MeshingEdgeCadIdELens = new List<CadIdELen>();
            foreach (var srcCadIdELen in src.MeshingEdgeCadIdELens)
            {
                MeshingEdgeCadIdELens.Add(new CadIdELen(srcCadIdELen));
            }
            MeshingLoopCadIdELens = new List<CadIdELen>();
            foreach (var srcCadIdELen in src.MeshingLoopCadIdELens)
            {
                MeshingLoopCadIdELens.Add(new CadIdELen(srcCadIdELen));
            }
            MeshingMode = src.MeshingMode;

            TypeLocs = new List<MeshTypeLoc>();
            foreach (var srcTypeLoc in src.TypeLocs)
            {
                TypeLocs.Add(new MeshTypeLoc(srcTypeLoc));
            }

            Vertexs = new List<MeshVertex2D>(src.Vertexs);
            BarArrays = new List<MeshBarArray2D>(src.BarArrays);
            TriArrays = new List<MeshTriArray2D>(src.TriArrays);

            Vecs = new List<OpenTK.Vector2d>(src.Vecs);
        }

        public void Clear()
        {
            MeshingEdgeCadIdELens.Clear();
            MeshingLoopCadIdELens.Clear();
            MeshingMode = 1;
            ClearMeshData();
        }

        private void ClearMeshData()
        {
            TypeLocs.Clear();

            Vertexs.Clear();
            BarArrays.Clear();
            TriArrays.Clear();

            Vecs.Clear();
        }

        public void AddMeshingLoopCadId(uint lCadId, double eLen)
        {
            int index = IndexOfMeshingLoopCadId(lCadId);
            if (index != -1)
            {
                return;
            }
            MeshingLoopCadIdELens.Add(new CadIdELen { CadId = lCadId, ELen = eLen });
        }

        public bool IsMeshingLoopCadId(uint lCadId)
        {
            int index = IndexOfMeshingLoopCadId(lCadId);
            return index != -1;
        }

        private int IndexOfMeshingLoopCadId(uint lCadId)
        {
            int index = -1;
            for (int i = 0; i < MeshingLoopCadIdELens.Count; i++)
            {
                var cadIdELen = MeshingLoopCadIdELens[i];
                if (cadIdELen.CadId == lCadId)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public void RemoveMeshingLoopCadId(uint lCadId)
        {
            int index = IndexOfMeshingLoopCadId(lCadId);
            if (index == -1)
            {
                return;
            }
            MeshingLoopCadIdELens.RemoveAt(index);
        }

        public void ClearMeshingLoopCadIds()
        {
            MeshingLoopCadIdELens.Clear();
        }

        public IList<uint> GetMeshingLoopCadIds()
        {
            IList<uint> lIds = new List<uint>();
            for (int i = 0; i < MeshingLoopCadIdELens.Count; i++)
            {
                var cadIdELen = MeshingLoopCadIdELens[i];
                lIds.Add(cadIdELen.CadId);
            }
            return lIds;
        }

        public IList<double> GetMeshingLoopCadELens()
        {
            IList<double> eLens = new List<double>();
            for (int i = 0; i < MeshingLoopCadIdELens.Count; i++)
            {
                var cadIdELen = MeshingLoopCadIdELens[i];
                eLens.Add(cadIdELen.ELen);
            }
            return eLens;
        }

        public void AddMeshingEdgeCadId(uint eCadId, double eLen)
        {
            int index = IndexOfMeshingEdgeCadId(eCadId);
            if (index != -1)
            {
                return;
            }
            MeshingEdgeCadIdELens.Add(new CadIdELen { CadId = eCadId, ELen = eLen });
        }

        public bool IsMeshingEdgeCadId(uint eCadId)
        {
            int index = IndexOfMeshingEdgeCadId(eCadId);
            return index != -1;
        }

        private int IndexOfMeshingEdgeCadId(uint eCadId)
        {
            int index = -1;
            for (int i = 0; i < MeshingEdgeCadIdELens.Count; i++)
            {
                var cadIdELen = MeshingEdgeCadIdELens[i];
                if (cadIdELen.CadId == eCadId)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public void RemoveMeshingEdgeCadId(uint eCadId)
        {
            int index = IndexOfMeshingLoopCadId(eCadId);
            if (index == -1)
            {
                return;
            }
            MeshingEdgeCadIdELens.RemoveAt(index);
        }

        public void ClearMeshingEdgeCadIds()
        {
            MeshingEdgeCadIdELens.Clear();
        }

        public IList<uint> GetMeshingEdgeCadIds()
        {
            IList<uint> eIds = new List<uint>();
            for (int i = 0; i < MeshingEdgeCadIdELens.Count; i++)
            {
                var cadIdELen = MeshingEdgeCadIdELens[i];
                eIds.Add(cadIdELen.CadId);
            }
            return eIds;
        }

        public IList<double> GetMeshingEdgeCadELens()
        {
            IList<double> eLens = new List<double>();
            for (int i = 0; i < MeshingEdgeCadIdELens.Count; i++)
            {
                var cadIdELen = MeshingEdgeCadIdELens[i];
                eLens.Add(cadIdELen.ELen);
            }
            return eLens;
        }

        public IList<MeshTriArray2D> GetTriArrays()
        {
            return TriArrays;
        }

        public IList<MeshBarArray2D> GetBarArrays()
        {
            return BarArrays;
        }

        public IList<MeshVertex2D> GetVertexs()
        {
            return Vertexs;
        }

        public IList<OpenTK.Vector2d> GetVectors()
        {
            return Vecs;
        }

        public void GetCoords(out IList<double> coord)
        {
            coord = new List<double>();
            uint nodeCnt = (uint)Vecs.Count;
            uint dim = 2;
            for (int i = 0; i < nodeCnt * dim; i++)
            {
                coord.Add(0.0);
            }
            for (int iNode = 0; iNode < nodeCnt; iNode++)
            {
                coord[(int)(iNode * dim)] = Vecs[iNode].X;
                coord[(int)(iNode * dim + 1)] = Vecs[iNode].Y;
            }
        }

        public IList<uint> GetIds()
        {
            IList<uint> ids = new List<uint>();
            for (uint id = 1; id < TypeLocs.Count; id++)
            {
                if (TypeLocs[(int)id].Loc == -1)
                {
                    continue;
                }
                ids.Add(id);
            }
            return ids;
        }

        public bool IsId(uint id)
        {
            if (TypeLocs.Count <= id)
            {
                return false;
            }
            int loc = TypeLocs[(int)id].Loc;
            if (loc == -1)
            {
                return false;
            }
            int type = TypeLocs[(int)id].Type;
            System.Diagnostics.Debug.Assert(type >= 0);
            System.Diagnostics.Debug.Assert(loc >= 0);
            if (type == 0)
            {
                System.Diagnostics.Debug.Assert(Vertexs.Count > loc);
                System.Diagnostics.Debug.Assert(Vertexs[loc].Id == id);
            }
            else if (type == 1)
            {
                System.Diagnostics.Debug.Assert(BarArrays.Count > loc);
                System.Diagnostics.Debug.Assert(BarArrays[loc].Id == id);
            }
            else if (type == 2)
            {
                System.Diagnostics.Debug.Assert(TriArrays.Count > loc);
                System.Diagnostics.Debug.Assert(TriArrays[loc].Id == id);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return true;
        }

        public IList<uint> GetIncludeElementIds(uint id)
        {
            IList<uint> ids = new List<uint>();
            if (id >= TypeLocs.Count)
            {
                return ids;
            }
            if (TypeLocs[(int)id].Loc == -1)
            {
                return ids;
            }
            return TypeLocs[(int)id].IncludeRelations;
        }

        public void GetInfo(uint id, out uint cadId, out int layer)
        {
            cadId = 0;
            layer = 0;

            int type = TypeLocs[(int)id].Type;
            int loc = TypeLocs[(int)id].Loc;
            if (type == 0)
            {
                cadId = Vertexs[loc].VCadId;
                layer = Vertexs[loc].Layer;
            }
            else if (type == 1)
            {
                cadId = BarArrays[loc].ECadId;
                layer = BarArrays[loc].Layer;
            }
            else if (type == 2)
            {
                cadId = TriArrays[loc].LCadId;
                layer = TriArrays[loc].Layer;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
        }

        public void GetConnectivity(uint id, out MeshType meshType, out int[] vertexs)
        {
            meshType = MeshType.NotSet;
            vertexs = null;

            System.Diagnostics.Debug.Assert(IsId(id));
            uint elemNodeCnt;
            uint elemCnt;
            int type = TypeLocs[(int)id].Type;
            int loc = TypeLocs[(int)id].Loc;
            System.Diagnostics.Debug.Assert(type != -1 && loc != -1);
            if (type == 0)
            {
                meshType = MeshType.Vertex;
                elemNodeCnt = 1;
                elemCnt = 1;
                vertexs = new int[elemNodeCnt * elemCnt];
                vertexs[0] = (int)Vertexs[loc].V;
            }
            else if (type == 1)
            {
                meshType = MeshType.Bar;
                elemNodeCnt = 2;
                IList<MeshBar> bars = BarArrays[loc].Bars;
                elemCnt = (uint)bars.Count;
                vertexs = new int[elemNodeCnt * elemCnt];
                for (int iElem = 0; iElem < elemCnt; iElem++)
                {
                    vertexs[iElem * 2] = (int)bars[iElem].V[0];
                    vertexs[iElem * 2 + 1] = (int)bars[iElem].V[1];
                }
            }
            else if (type == 2)
            {
                meshType = MeshType.Tri;
                elemNodeCnt = 3;
                IList<MeshTri2D> tris = TriArrays[loc].Tris;
                elemCnt = (uint)tris.Count;
                vertexs = new int[elemNodeCnt * elemCnt];
                for (int iElem = 0; iElem < elemCnt; iElem++)
                {
                    vertexs[iElem * 3] = (int)tris[iElem].V[0];
                    vertexs[iElem * 3 + 1] = (int)tris[iElem].V[1];
                    vertexs[iElem * 3 + 2] = (int)tris[iElem].V[2];
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
        }

        public bool GetMeshInfo(uint id,
            out uint elemCount, out MeshType meshType, out int loc, out uint cadId)
        {
            elemCount = 0;
            meshType = MeshType.NotSet;
            loc = 0;
            cadId = 0;

            if (!IsId(id))
            {
                return false;
            }
            System.Diagnostics.Debug.Assert(id < TypeLocs.Count);
            int type = TypeLocs[(int)id].Type;
            loc = TypeLocs[(int)id].Loc;
            System.Diagnostics.Debug.Assert(type >= 0);
            if (type == 0)
            {
                System.Diagnostics.Debug.Assert(loc < Vertexs.Count);
                MeshVertex2D vertex = Vertexs[loc];
                cadId = vertex.VCadId;
                elemCount = 1;
                meshType = MeshType.Vertex;
            }
            else if (type == 1)
            {
                System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                MeshBarArray2D barArray = BarArrays[loc];
                cadId = barArray.ECadId;
                elemCount = (uint)barArray.Bars.Count;
                meshType = MeshType.Bar;
            }
            else if (type == 2)
            {
                System.Diagnostics.Debug.Assert(loc < TriArrays.Count);
                MeshTriArray2D triArray = TriArrays[loc];
                cadId = triArray.LCadId;
                elemCount = (uint)triArray.Tris.Count;
                meshType = MeshType.Tri;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
                throw new NotImplementedException();
            }
            return true;
        }

        private uint FindMaxId()
        {
            uint maxId = 0;
            {
                for (uint iVer = 0; iVer < Vertexs.Count; iVer++)
                {
                    if (maxId < Vertexs[(int)iVer].Id)
                    {
                        maxId = Vertexs[(int)iVer].Id;
                    }
                }
                for (uint iBarArray = 0; iBarArray < BarArrays.Count; iBarArray++)
                {
                    if (maxId < BarArrays[(int)iBarArray].Id)
                    {
                        maxId = BarArrays[(int)iBarArray].Id;
                    }
                }
                for (uint iTriArray = 0; iTriArray < TriArrays.Count; iTriArray++)
                {
                    if (maxId < TriArrays[(int)iTriArray].Id)
                    {
                        maxId = TriArrays[(int)iTriArray].Id;
                    }
                }
            }
            return maxId;
        }

        private uint GetFreeObjectId()
        {
            uint maxId = FindMaxId();
            IList<uint> isUsedFlgs = new List<uint>();
            {
                for (uint iUse = 0; iUse < maxId + 1; iUse++)
                {
                    isUsedFlgs.Add(0);
                }
                for (uint iVer = 0; iVer < Vertexs.Count; iVer++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)Vertexs[(int)iVer].Id] == 0);
                    System.Diagnostics.Debug.Assert(Vertexs[(int)iVer].Id >= 1 && Vertexs[(int)iVer].Id <= maxId);
                    isUsedFlgs[(int)Vertexs[(int)iVer].Id] = 1;
                }
                for (uint iBarArray = 0; iBarArray < BarArrays.Count; iBarArray++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)BarArrays[(int)iBarArray].Id] == 0);
                    System.Diagnostics.Debug.Assert(BarArrays[(int)iBarArray].Id >= 1 && BarArrays[(int)iBarArray].Id <= maxId);
                    isUsedFlgs[(int)BarArrays[(int)iBarArray].Id] = 1;
                }
                for (uint iTriArray = 0; iTriArray < TriArrays.Count; iTriArray++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)TriArrays[(int)iTriArray].Id] == 0);
                    System.Diagnostics.Debug.Assert(TriArrays[(int)iTriArray].Id >= 1 && TriArrays[(int)iTriArray].Id <= maxId);
                    isUsedFlgs[(int)TriArrays[(int)iTriArray].Id] = 1;
                }
            }
            System.Diagnostics.Debug.Assert(isUsedFlgs[0] == 0);
            for (uint i = 1; i < isUsedFlgs.Count; i++)
            {
                if (isUsedFlgs[(int)i] == 0)
                {
                    return i;
                }
            }
            return maxId + 1;
        }

        public uint GetIdFromCadId(uint cadId, CadElementType cadType)
        {
            switch (cadType)
            {
                case CadElementType.Vertex:
                    for (uint iVer = 0; iVer < Vertexs.Count; iVer++)
                    {
                        if (Vertexs[(int)iVer].VCadId == cadId)
                        {
                            uint meshId = Vertexs[(int)iVer].Id;
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Loc == iVer);
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Type == 0); // VERTEX
                            return meshId;
                        }
                    }
                    break;

                case CadElementType.Edge:
                    for (uint iBar = 0; iBar < BarArrays.Count; iBar++)
                    {
                        if (BarArrays[(int)iBar].ECadId == cadId)
                        {
                            uint meshId = BarArrays[(int)iBar].Id;
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Loc == iBar);
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Type == 1); // BAR
                            return meshId;
                        }
                    }
                    break;

                case CadElementType.Loop:
                    for (uint iTri = 0; iTri < TriArrays.Count; iTri++)
                    {
                        if (TriArrays[(int)iTri].LCadId == cadId)
                        {
                            uint meshId = TriArrays[(int)iTri].Id;
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Loc == iTri);
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Type == 2); // TRI
                            return meshId;
                        }
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return 0;
            }
            return 0;
        }

        private bool FindElemLocTypeFromCadIdType(CadElementType cadType, uint cadId, out uint loc, out uint type)
        {
            loc = 0;
            type = 0;

            switch (cadType)
            {
                case CadElementType.Vertex:
                    for (uint iVer = 0; iVer < Vertexs.Count; iVer++)
                    {
                        if (Vertexs[(int)iVer].VCadId == cadId)
                        {
                            loc = iVer;
                            type = 0; // VERTEX
                            uint meshId = Vertexs[(int)iVer].Id;
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Loc == loc);
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Type == type);
                            return true;
                        }
                    }
                    break;

                case CadElementType.Edge:
                    for (uint iBar = 0; iBar < BarArrays.Count; iBar++)
                    {
                        if (BarArrays[(int)iBar].ECadId == cadId)
                        {
                            loc = iBar;
                            type = 1; // BAR
                            uint meshId = BarArrays[(int)iBar].Id;
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Loc == loc);
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Type == type);
                            return true;
                        }
                    }
                    break;

                case CadElementType.Loop:
                    for (uint iTri = 0; iTri < TriArrays.Count; iTri++)
                    {
                        if (TriArrays[(int)iTri].LCadId == cadId)
                        {
                            loc = iTri;
                            type = 2; // TRI
                            uint meshId = TriArrays[(int)iTri].Id;
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Loc == loc);
                            System.Diagnostics.Debug.Assert(TypeLocs[(int)meshId].Type == type);
                            return true;
                        }
                    }
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
            return false;
        }

        public void SetMeshingModeZero()
        {
            MeshingMode = 0;
        }

        public void SetMeshingModeElemLength()
        {
            MeshingMode = 1;
        }

        public bool MakeMesh(Cad2D cad)
        {
            Cad = cad;

            IList<uint> meshingLoopIds = new List<uint>();
            IList<double> meshingLoopELens = new List<double>();
            foreach (var cadIdELen in MeshingLoopCadIdELens)
            {
                uint lId = cadIdELen.CadId;
                double eLen = cadIdELen.ELen;
                if (!cad.IsElementId(CadElementType.Loop, lId))
                {
                    continue;
                }
                meshingLoopIds.Add(lId);
                meshingLoopELens.Add(eLen);
            }
            IList<uint> meshingEdgeIds = new List<uint>();
            IList<double> meshingEdgeELens = new List<double>();
            foreach (var cadIdELen in MeshingEdgeCadIdELens)
            {
                uint eId = cadIdELen.CadId;
                double eLen = cadIdELen.ELen;
                if (!cad.IsElementId(CadElementType.Edge, eId))
                {
                    continue;
                }
                meshingEdgeIds.Add(eId);
                meshingEdgeELens.Add(eLen);
            }

            if (MeshingMode == 0)
            {
                return Tessellation(cad, meshingLoopIds);
            }
            else if (MeshingMode == 1)
            {
                return MakeMeshElemLength(cad, meshingLoopIds, meshingLoopELens, meshingEdgeIds, meshingEdgeELens);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return false;
        }

        private bool Tessellation(Cad2D cad, IList<uint> loopIds)
        {
            // VERTEX
            {
                IList<uint> vIds = cad.GetElementIds(CadElementType.Vertex);
                for (uint iV = 0; iV < vIds.Count; iV++)
                {
                    uint vId = vIds[(int)iV];
                    System.Diagnostics.Debug.Assert(GetIdFromCadId(vId, CadElementType.Vertex) == 0);
                    uint addId = GetFreeObjectId();
                    OpenTK.Vector2d vec = cad.GetVertexCoord(vId);
                    Vecs.Add(vec);
                    {
                        MeshVertex2D tmpVer = new MeshVertex2D();
                        tmpVer.Id = addId;
                        tmpVer.VCadId = vId;
                        tmpVer.V = (uint)(Vecs.Count - 1);
                        Vertexs.Add(tmpVer);
                    }
                    {
                        int typeLocCnt = TypeLocs.Count;
                        for (int i = typeLocCnt; i < addId + 1; i++)
                        {
                            var typeLoc = new MeshTypeLoc { Type = 0, Loc = -1 };
                            TypeLocs.Add(typeLoc);
                        }
                        TypeLocs[(int)addId].Loc = Vertexs.Count - 1;
                        TypeLocs[(int)addId].Type = 0; // VERTEX
                    }
                    System.Diagnostics.Debug.Assert(CheckMesh() == 0);
                }
            }

            // EDGE
            {
                IList<uint> eIds = cad.GetElementIds(CadElementType.Edge);
                for (uint iE = 0; iE < eIds.Count; iE++)
                {
                    uint eId = eIds[(int)iE];

                    TessellateEdge(cad, eId);

                    System.Diagnostics.Debug.Assert(CheckMesh() == 0);
                }
            }

            // LOOP
            {
                for (uint iL = 0; iL < loopIds.Count; iL++)
                {
                    uint lId = loopIds[(int)iL];

                    TessellateLoop(cad, lId);

                    System.Diagnostics.Debug.Assert(CheckMesh() == 0);
                }
            }

            MakeIncludeRelation(cad);

            return true;
        }

        private bool TessellateEdge(Cad2D cad, uint eId)
        {
            uint sVId;
            uint eVId;
            System.Diagnostics.Debug.Assert(cad.IsElementId(CadElementType.Edge, eId));
            if (!cad.GetEdgeVertexId(eId, out sVId, out eVId))
            {
                System.Diagnostics.Debug.WriteLine("error edge : " + eId);
                System.Diagnostics.Debug.Assert(false);
            }

            uint iSP;
            uint iEP;
            uint sMeshId;
            uint eMeshId;
            {
                uint loc;
                uint type;
                if (!FindElemLocTypeFromCadIdType(CadElementType.Vertex, sVId, out loc, out type))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 0 && loc < Vertexs.Count);
                MeshVertex2D sVer = Vertexs[(int)loc];
                iSP = sVer.V;
                sMeshId = sVer.Id;
                if (!FindElemLocTypeFromCadIdType(CadElementType.Vertex, eVId, out loc, out type))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 0 && loc < Vertexs.Count);

                MeshVertex2D eVer = Vertexs[(int)loc];
                iEP = eVer.V;
                eMeshId = eVer.Id;
            }

            uint newElemArrayId = GetFreeObjectId();
            uint ibarArary0 = (uint)BarArrays.Count;
            {
                int typeLocCnt = TypeLocs.Count;
                for (int i = typeLocCnt; i < newElemArrayId + 1; i++)
                {
                    var typeLoc = new MeshTypeLoc { Type = 0, Loc = -1 };
                    TypeLocs.Add(typeLoc);
                }
                TypeLocs[(int)newElemArrayId].Loc = (int)ibarArary0;
                TypeLocs[(int)newElemArrayId].Type = 1; // BAR
            }
            BarArrays.Add(new MeshBarArray2D());
            MeshBarArray2D barArray = BarArrays[(int)ibarArary0];
            IList<OpenTK.Vector2d> pts;
            cad.GetCurveAsPolyline(eId, out pts, -1);

            uint div = (uint)(pts.Count + 1);
            IList<uint> iPts = new List<uint>();
            {
                for (int i = 0; i < div + 1; i++)
                {
                    iPts.Add(0);
                }
                iPts[0] = iSP;
                for (uint i = 1; i < div; i++)
                {
                    iPts[(int)i] = (uint)Vecs.Count;
                    Vecs.Add(pts[(int)(i - 1)]);
                }
                iPts[(int)div] = iEP;
            }
            {
                barArray.Id = newElemArrayId;
                barArray.ECadId = eId;
                barArray.Layer = cad.GetLayer(CadElementType.Edge, eId);
                int barCnt = barArray.Bars.Count;
                for (int i = barCnt; i < div; i++)
                {
                    barArray.Bars.Add(new MeshBar());
                }
                barArray.SEId[0] = sMeshId;
                barArray.SEId[1] = eMeshId;
                barArray.LRId[0] = 0;
                barArray.LRId[1] = 0;
                for (uint iBar = 0; iBar < div; iBar++)
                {
                    barArray.Bars[(int)iBar].V[0] = iPts[(int)iBar];
                    barArray.Bars[(int)iBar].V[1] = iPts[(int)(iBar + 1)];
                    barArray.Bars[(int)iBar].S2[0] = 0;
                    barArray.Bars[(int)iBar].S2[1] = 0;
                    barArray.Bars[(int)iBar].R2[0] = 0;
                    barArray.Bars[(int)iBar].R2[1] = 0;
                }
            }
            System.Diagnostics.Debug.Assert(CheckMesh() == 0);

            return true;
        }

        private bool TessellateLoop(Cad2D cad, uint lId)
        {
            IList<MeshPoint2D> points = new List<MeshPoint2D>();
            IList<int> vec2Pt = new List<int>();
            {
                // 要素分割する領域の節点　Pt2Dsを作成
                // 辺に属する節点の全体番号から、要素分割する領域のローカル番号への対応(vec2Pt)を作成

                ////////////////////////////////
                for (int i = 0; i < Vecs.Count; i++)
                {
                    vec2Pt.Add(-1);
                }
                {
                    // このループで使用される節点のフラグを立てる
                    LoopEdgeItr loopItr = cad.GetLoopEdgeItr(lId);
                    while (true)
                    {
                        // ループをめぐる
                        for (; !loopItr.IsEnd(); loopItr.Next())
                        {
                            // このループの中のエッジをめぐる
                            {
                                uint vId = loopItr.GetVertexId();
                                uint locTmp;
                                uint typeTmp;
                                if (!FindElemLocTypeFromCadIdType(CadElementType.Vertex, vId, out locTmp, out typeTmp))
                                {
                                    System.Diagnostics.Debug.Assert(false);
                                }
                                System.Diagnostics.Debug.Assert(typeTmp == 0 && locTmp < Vertexs.Count);
                                MeshVertex2D ver = Vertexs[(int)locTmp];
                                vec2Pt[(int)ver.V] = 1;
                            }
                            uint eId;
                            bool isSameDir;
                            if (!loopItr.GetEdgeId(out eId, out isSameDir))
                            {
                                continue; // 浮遊点は飛ばす
                            }
                            uint type;
                            uint loc;
                            if (!FindElemLocTypeFromCadIdType(CadElementType.Edge, eId, out loc, out type))
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                            System.Diagnostics.Debug.Assert(type == 1);
                            MeshBarArray2D barArray = BarArrays[(int)loc];
                            System.Diagnostics.Debug.Assert(barArray.ECadId == eId);
                            IList<MeshBar> bars = barArray.Bars;
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                vec2Pt[(int)bars[(int)ibar].V[0]] = 1;
                                vec2Pt[(int)bars[(int)ibar].V[1]] = 1;
                            }
                        }
                        if (!loopItr.ShiftChildLoop())
                        {
                            break;
                        }
                    }
                }
                {
                    // vec2Ptを作る、pointsを確保する
                    int iPt = 0;
                    for (uint iVec = 0; iVec < vec2Pt.Count; iVec++)
                    {
                        if (vec2Pt[(int)iVec] != -1)
                        {
                            vec2Pt[(int)iVec] = iPt;
                            iPt++;
                        }
                    }
                    int ptCnt = points.Count;
                    for (int i = ptCnt; i < iPt; i++)
                    {
                        points.Add(new MeshPoint2D());
                    }
                }
                for (uint iVec = 0; iVec < vec2Pt.Count; iVec++)
                {
                    if (vec2Pt[(int)iVec] != -1)
                    {
                        uint ip = (uint)vec2Pt[(int)iVec];
                        System.Diagnostics.Debug.Assert(ip < points.Count);
                        points[(int)ip].Point = new OpenTK.Vector2d(Vecs[(int)iVec].X, Vecs[(int)iVec].Y);
                        points[(int)ip].Element = -1;
                        points[(int)ip].Node = 0;
                    }
                }
            }

            IList<MeshTri2D> tris = new List<MeshTri2D>();
            {
                // 与えられた点群を内部に持つ、大きな三角形を作る
                System.Diagnostics.Debug.Assert(Vecs.Count >= 3);
                double maxLen;
                double[] center = new double[2];
                {
                    double[] bound2d = new double[4];
                    bound2d[0] = points[0].Point.X;
                    bound2d[1] = points[0].Point.X;
                    bound2d[2] = points[0].Point.Y;
                    bound2d[3] = points[0].Point.Y;
                    for (uint ipoin = 1; ipoin < points.Count; ipoin++)
                    {
                        if (points[(int)ipoin].Point.X < bound2d[0])
                        {
                            bound2d[0] = points[(int)ipoin].Point.X;
                        }
                        if (points[(int)ipoin].Point.X > bound2d[1])
                        {
                            bound2d[1] = points[(int)ipoin].Point.X;
                        }
                        if (points[(int)ipoin].Point.Y < bound2d[2])
                        {
                            bound2d[2] = points[(int)ipoin].Point.Y;
                        }
                        if (points[(int)ipoin].Point.Y > bound2d[3])
                        {
                            bound2d[3] = points[(int)ipoin].Point.Y;
                        }
                    }
                    maxLen = (bound2d[1] - bound2d[0] > bound2d[3] - bound2d[2]) ?
                        bound2d[1] - bound2d[0] : bound2d[3] - bound2d[2];
                    center[0] = (bound2d[1] + bound2d[0]) * 0.5;
                    center[1] = (bound2d[3] + bound2d[2]) * 0.5;
                }

                double triLen = maxLen * 8.0;
                double tmpLen = triLen * Math.Sqrt(3.0) / 6.0;

                int ptCnt = points.Count;
                for (int i = ptCnt; i < ptCnt + 3; i++)
                {
                    points.Add(new MeshPoint2D());
                }
                points[ptCnt + 0].Point = new OpenTK.Vector2d(
                    center[0],
                    (center[1] + 2.0 * tmpLen));
                points[ptCnt + 0].Element = 0;
                points[ptCnt + 0].Node = 0;
                points[ptCnt + 1].Point = new OpenTK.Vector2d(
                    (center[0] - 0.5 * triLen),
                    (center[1] - tmpLen));
                points[ptCnt + 1].Element = 0;
                points[ptCnt + 1].Node = 1;
                points[ptCnt + 2].Point = new OpenTK.Vector2d(
                    (center[0] + 0.5 * triLen),
                    (center[1] - tmpLen));
                points[ptCnt + 2].Element = 0;
                points[ptCnt + 2].Node = 2;

                int triCnt = tris.Count;
                for (int i = triCnt; i < 1; i++)
                {
                    tris.Add(new MeshTri2D());
                }
                tris[0].V[0] = (uint)(ptCnt + 0);
                tris[0].V[1] = (uint)(ptCnt + 1);
                tris[0].V[2] = (uint)(ptCnt + 2);
                tris[0].G2[0] = -1;
                tris[0].G2[1] = -1;
                tris[0].G2[2] = -1;
                tris[0].S2[0] = 0;
                tris[0].S2[1] = 0;
                tris[0].S2[2] = 0;
                tris[0].R2[0] = 0;
                tris[0].R2[1] = 0;
                tris[0].R2[2] = 0;
            }

            // Make Delaunay Division
            for (uint iPt = 0; iPt < points.Count; iPt++)
            {
                if (points[(int)iPt].Element >= 0)
                {
                    continue;  // 既にメッシュの一部である。
                }
                OpenTK.Vector2d addPo = points[(int)iPt].Point;
                int iInTri = -1;
                int iEdge = -1;
                uint iflg1 = 0;
                uint iflg2 = 0;
                for (uint iTri = 0; iTri < tris.Count; iTri++)
                {
                    iflg1 = 0;
                    iflg2 = 0;
                    MeshTri2D tri = tris[(int)iTri];
                    if (CadUtils2D.TriArea(addPo,
                        points[(int)tri.V[1]].Point, points[(int)tri.V[2]].Point) > MinTriArea)
                    {
                        iflg1++;
                        iflg2 += 0;
                    }
                    if (CadUtils2D.TriArea(addPo,
                        points[(int)tri.V[2]].Point, points[(int)tri.V[0]].Point) > MinTriArea)
                    {
                        iflg1++;
                        iflg2 += 1;
                    }
                    if (CadUtils2D.TriArea(addPo,
                        points[(int)tri.V[0]].Point, points[(int)tri.V[1]].Point) > MinTriArea)
                    {
                        iflg1++; iflg2 += 2;
                    }
                    if (iflg1 == 3)
                    {
                        iInTri = (int)iTri;
                        break;
                    }
                    else if (iflg1 == 2)
                    {
                        uint iEd0 = 3 - iflg2;
                        uint iEPt0 = tri.V[MeshUtils2D.TriElEdgeNo[iEd0][0]];
                        uint iEPt1 = tri.V[MeshUtils2D.TriElEdgeNo[iEd0][1]];
                        uint[] rel = MeshUtils2D.RelTriTri[tri.R2[iEd0]];
                        uint iTriS = tri.S2[iEd0];
                        System.Diagnostics.Debug.Assert(
                            tris[(int)iTriS].V[rel[MeshUtils2D.TriElEdgeNo[iEd0][0]]] == iEPt0);
                        System.Diagnostics.Debug.Assert(
                            tris[(int)iTriS].V[rel[MeshUtils2D.TriElEdgeNo[iEd0][1]]] == iEPt1);
                        uint inoel_d = rel[iEd0];
                        System.Diagnostics.Debug.Assert(tris[(int)iTriS].S2[inoel_d] == iTri);
                        uint ipo_d = tris[(int)iTriS].V[inoel_d];
                        System.Diagnostics.Debug.Assert(
                            CadUtils2D.TriArea(addPo, points[(int)iEPt1].Point,
                            points[(int)tris[(int)iTri].V[iEd0]].Point) > MinTriArea);
                        System.Diagnostics.Debug.Assert(
                            CadUtils2D.TriArea(addPo,
                            points[(int)tris[(int)iTri].V[iEd0]].Point,
                            points[(int)iEPt0].Point) > MinTriArea);
                        if (CadUtils2D.TriArea(addPo,
                            points[(int)iEPt0].Point, points[(int)ipo_d].Point) < MinTriArea)
                        {
                            continue;
                        }
                        if (CadUtils2D.TriArea(addPo,
                            points[(int)ipo_d].Point, points[(int)iEPt1].Point) < MinTriArea)
                        {
                            continue;
                        }
                        int detD = MeshUtils2D.DetDelaunay(addPo,
                            points[(int)iEPt0].Point, points[(int)iEPt1].Point, points[(int)ipo_d].Point);
                        if (detD == 2 || detD == 1)
                        {
                            continue;
                        }
                        iInTri = (int)iTri;
                        iEdge = (int)iEd0;
                        break;
                    }
                }
                if (iInTri == -1)
                {
                    System.Diagnostics.Debug.WriteLine("Super Triangle Failure " + iPt + " (" +
                        addPo.X + " " + addPo.Y + ")");
                    System.Diagnostics.Debug.WriteLine(tris.Count);
                    return false;
                }
                if (iEdge == -1)
                {
                    MeshUtils2D.InsertPointElem(iPt, (uint)iInTri, points, tris);
                }
                else
                {
                    MeshUtils2D.InsertPointElemEdge(iPt, (uint)iInTri, (uint)iEdge, points, tris);
                }
                MeshUtils2D.DelaunayAroundPoint(iPt, points, tris);
            }
            System.Diagnostics.Debug.Assert(MeshUtils2D.CheckTri(points, tris));

            uint newTriId = GetFreeObjectId();

            {
                // エッジを回復する
                LoopEdgeItr loopItr = cad.GetLoopEdgeItr(lId);
                while (true)
                {
                    // 子ループのためのループ
                    for (; !loopItr.IsEnd(); loopItr.Next())
                    {
                        uint eId;
                        bool isSameDir;
                        if (!loopItr.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;  // ループの中の点
                        }
                        uint loc;
                        uint type;
                        if (!FindElemLocTypeFromCadIdType(CadElementType.Edge, eId, out loc, out type))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                        System.Diagnostics.Debug.Assert(type == 1);
                        MeshBarArray2D barArray = BarArrays[(int)loc];
                        System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                        IList<MeshBar> bars = barArray.Bars;
                        uint barArrayId = barArray.Id;
                        System.Diagnostics.Debug.Assert(barArrayId != newTriId);
                        for (uint ibar = 0; ibar < bars.Count; ibar++)
                        {
                            while (true)
                            {
                                // EdgeをFlipしたら同じ辺について繰り返す				

                                // ipoi0は左周りのbarの始点、ipoi1は終点
                                uint ipoi0;
                                uint ipoi1;
                                if (isSameDir)
                                {
                                    ipoi0 = (uint)vec2Pt[(int)bars[(int)ibar].V[0]];
                                    ipoi1 = (uint)vec2Pt[(int)bars[(int)ibar].V[1]];
                                }
                                else
                                {
                                    ipoi0 = (uint)vec2Pt[(int)bars[(int)ibar].V[1]];
                                    ipoi1 = (uint)vec2Pt[(int)bars[(int)ibar].V[0]];
                                }
                                System.Diagnostics.Debug.Assert(ipoi0 < points.Count);
                                System.Diagnostics.Debug.Assert(ipoi1 < points.Count);

                                uint iTri0;
                                uint iTriNo0;
                                uint iTriNo1;
                                if (MeshUtils2D.FindEdge(ipoi0, ipoi1, out iTri0, out iTriNo0, out iTriNo1,
                                    points, tris))
                                {
                                    // ループの内側に接する要素を見つける
                                    // Split Triangle
                                    System.Diagnostics.Debug.Assert(iTriNo0 != iTriNo1);
                                    System.Diagnostics.Debug.Assert(iTriNo0 < 3);
                                    System.Diagnostics.Debug.Assert(iTriNo1 < 3);
                                    System.Diagnostics.Debug.Assert(tris[(int)iTri0].V[iTriNo0] == ipoi0);
                                    System.Diagnostics.Debug.Assert(tris[(int)iTri0].V[iTriNo1] == ipoi1);
                                    uint ied0 = 3 - iTriNo0 - iTriNo1;
                                    {
                                        uint iTri1 = tris[(int)iTri0].S2[ied0];
                                        uint iEd1 = (uint)MeshUtils2D.RelTriTri[(int)tris[(int)iTri0].R2[ied0]][ied0];
                                        System.Diagnostics.Debug.Assert(tris[(int)iTri1].S2[iEd1] == iTri0);
                                        tris[(int)iTri1].G2[iEd1] = -3;
                                        tris[(int)iTri0].G2[ied0] = -3;
                                    }
                                    break;  // 次のBarへ　for(;;)を抜ける
                                }
                                else
                                {
                                    double ratio;
                                    if (!MeshUtils2D.FindEdgePointAcrossEdge(ipoi0, ipoi1,
                                        out iTri0, out iTriNo0, out iTriNo1, out ratio,
                                        points, tris))
                                    {
                                        System.Diagnostics.Debug.WriteLine("歪んだメッシュ");
                                        return false;
                                    }
                                    // return false if degeneration
                                    if (ratio < -1.0e-20 || ratio > 1.0 + 1.0e-20)
                                    {
                                        return false;
                                    }
                                    if (CadUtils2D.TriArea(
                                        points[(int)ipoi0].Point,
                                        points[(int)tris[(int)iTri0].V[iTriNo0]].Point,
                                        points[(int)ipoi1].Point) < 1.0e-20)
                                    {
                                        return false;
                                    }
                                    if (CadUtils2D.TriArea(
                                        points[(int)ipoi0].Point,
                                        points[(int)ipoi1].Point,
                                        points[(int)tris[(int)iTri0].V[iTriNo1]].Point) < 1.0e-20)
                                    {
                                        return false;
                                    }
                                    /*
                                    System.Diagnostics.Debug.Assert(ratio > -1.0e-20 && ratio < 1.0 + 1.0e-20);
                                    System.Diagnostics.Debug.Assert(CadUtils.TriArea(
                                        pt2Ds[(int)ipoi0].Point,
                                        pt2Ds[(int)tris[(int)itri0].V[inotri0]].Point,
                                        pt2Ds[(int)ipoi1].Point) > 1.0e-20);
                                    System.Diagnostics.Debug.Assert(CadUtils.TriArea(
                                        pt2Ds[(int)ipoi0].Point,
                                        pt2Ds[(int)ipoi1].Point,
                                        pt2Ds[(int)tris[(int)itri0].V[inotri1]].Point) > 1.0e-20);
                                    */

                                    if (ratio < 1.0e-20)
                                    {
                                        // "未実装 辺上に点がある場合"
                                        return false;
                                    }
                                    else if (ratio > 1.0 - 1.0e-10)
                                    {
                                        //	"未実装 辺上に点がある場合"
                                        return false;
                                    }
                                    else
                                    {
                                        uint ied0 = 3 - iTriNo0 - iTriNo1;
                                        if (tris[(int)iTri0].G2[ied0] != -2)
                                        {
                                            return false;
                                        }
                                        System.Diagnostics.Debug.Assert(tris[(int)iTri0].G2[ied0] == -2);
                                        uint itri1 = tris[(int)iTri0].S2[ied0];
                                        uint ied1 = (uint)MeshUtils2D.RelTriTri[tris[(int)iTri0].R2[ied0]][ied0];
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == iTri0);
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].G2[ied1] == -2);
                                        MeshUtils2D.FlipEdge(iTri0, ied0, points, tris);
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    if (!loopItr.ShiftChildLoop())
                    {
                        break;
                    }
                }
                System.Diagnostics.Debug.Assert(MeshUtils2D.CheckTri(points, tris));
            }

            ////////////////////////////////////////////////
            // ここからはクラスの内容を変更する
            // エラーを出して戻るなら、ここ以前にすること
            ////////////////////////////////////////////////

            // ここから辺要素の隣接関係を変更する．三角形についてはそのまま

            {
                // 辺要素から三角形要素への隣接情報を作成
                LoopEdgeItr loopItr = cad.GetLoopEdgeItr(lId);
                while (true)
                {   
                    // 子ループのためのループ
                    for (; !loopItr.IsEnd(); loopItr.Next())
                    {
                        uint eId;
                        bool isSameDir;
                        if (!loopItr.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;  // ループの中の点
                        }
                        uint loc;
                        uint type;
                        if (!FindElemLocTypeFromCadIdType(CadElementType.Edge, eId, out loc, out type))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                        System.Diagnostics.Debug.Assert(type == 1);
                        MeshBarArray2D barArray = BarArrays[(int)loc];
                        System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                        IList<MeshBar> bars = barArray.Bars;
                        uint barArrayId = barArray.Id;
                        System.Diagnostics.Debug.Assert(barArrayId != newTriId);
                        for (uint ibar = 0; ibar < bars.Count; ibar++)
                        {
                            // ipoi0は左周りのbarの始点、ipoi1は終点
                            uint ipoi0;
                            uint ipoi1;
                            if (isSameDir)
                            {
                                ipoi0 = (uint)vec2Pt[(int)bars[(int)ibar].V[0]];
                                ipoi1 = (uint)vec2Pt[(int)bars[(int)ibar].V[1]];
                            }
                            else
                            {
                                ipoi0 = (uint)vec2Pt[(int)bars[(int)ibar].V[1]];
                                ipoi1 = (uint)vec2Pt[(int)bars[(int)ibar].V[0]];
                            }
                            System.Diagnostics.Debug.Assert(ipoi0 < points.Count);
                            System.Diagnostics.Debug.Assert(ipoi1 < points.Count);
                            //
                            uint iTri0;
                            uint iTriNo0;
                            uint iTriNo1;
                            // ループの内側に接する要素を見つける
                            if (!MeshUtils2D. FindEdge(ipoi0, ipoi1, out iTri0, out iTriNo0, out iTriNo1,
                                points, tris))
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            System.Diagnostics.Debug.Assert(iTriNo0 != iTriNo1);
                            System.Diagnostics.Debug.Assert(iTriNo0 < 3);
                            System.Diagnostics.Debug.Assert(iTriNo1 < 3);
                            System.Diagnostics.Debug.Assert(tris[(int)iTri0].V[iTriNo0] == ipoi0);
                            System.Diagnostics.Debug.Assert(tris[(int)iTri0].V[iTriNo1] == ipoi1);
                            uint ied0 = 3 - iTriNo0 - iTriNo1;
                            // 辺要素の隣接情報を作る
                            if (isSameDir)
                            {
                                System.Diagnostics.Debug.Assert(barArray.LRId[0] == newTriId ||
                                    barArray.LRId[0] == 0);
                                barArray.LRId[0] = newTriId;
                                bars[(int)ibar].S2[0] = iTri0; bars[(int)ibar].R2[0] = ied0;
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(barArray.LRId[1] == newTriId ||
                                    barArray.LRId[1] == 0);
                                barArray.LRId[1] = newTriId;
                                bars[(int)ibar].S2[1] = iTri0;
                                bars[(int)ibar].R2[1] = ied0;
                            }
                        }
                    }
                    if (!loopItr.ShiftChildLoop())
                    {
                        break;
                    }
                }
                System.Diagnostics.Debug.Assert(MeshUtils2D.CheckTri(points, tris));
            }

            // 今後は辺要素を変更するのは，TriAryの番号付けを変化させるとき

            {
                // 辺との隣接番号の整合性をとる
                LoopEdgeItr loopItr = cad.GetLoopEdgeItr(lId);
                while (true)
                {
                    // 子ループのためのループ
                    for (; !loopItr.IsEnd(); loopItr.Next())
                    {
                        uint eId;
                        bool isSameDir;
                        if (!loopItr.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;  // 子ループが点の場合
                        }
                        uint loc;
                        uint type;
                        if (!FindElemLocTypeFromCadIdType(CadElementType.Edge, eId, out loc, out type))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                        System.Diagnostics.Debug.Assert(type == 1);
                        MeshBarArray2D barArray = BarArrays[(int)loc];
                        System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                        IList<MeshBar> bars = barArray.Bars;
                        uint barArrayId = barArray.Id;
                        System.Diagnostics.Debug.Assert(barArrayId != newTriId);
                        if (barArray.LRId[0] == newTriId)
                        {
                            // 左側を切り離す
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                MeshBar bar = bars[(int)ibar];
                                uint iTri0 = bar.S2[0];
                                uint iEd0 = bar.R2[0];
                                System.Diagnostics.Debug.Assert(iTri0 < tris.Count);
                                System.Diagnostics.Debug.Assert(iEd0 < 3);
                                System.Diagnostics.Debug.Assert(tris[(int)iTri0].V[MeshUtils2D.TriElEdgeNo[iEd0][0]] ==
                                    vec2Pt[(int)bar.V[0]] ||
                                    tris[(int)iTri0].V[MeshUtils2D.TriElEdgeNo[iEd0][0]] ==
                                    vec2Pt[(int)bar.V[1]]);
                                System.Diagnostics.Debug.Assert(tris[(int)iTri0].V[MeshUtils2D.TriElEdgeNo[iEd0][1]] ==
                                    vec2Pt[(int)bar.V[0]] ||
                                    tris[(int)iTri0].V[MeshUtils2D.TriElEdgeNo[iEd0][1]] ==
                                    vec2Pt[(int)bar.V[1]]);
                                if (tris[(int)iTri0].G2[iEd0] == barArrayId)
                                {
                                    continue; // すでに切り離されてる
                                }
                                {
                                    // 向かい側の要素の処理
                                    uint itri1 = tris[(int)iTri0].S2[iEd0];
                                    uint ied1 = MeshUtils2D.RelTriTri[tris[(int)iTri0].R2[iEd0]][iEd0];
                                    System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == iTri0);
                                    if (barArray.LRId[1] != newTriId)
                                    {
                                        // 外側の要素を切り離す
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == iTri0);
                                        tris[(int)itri1].G2[ied1] = -1;
                                    }
                                    else
                                    {
                                        // 辺をはさんで向かい側の要素も内側だから辺にくっつける
                                        tris[(int)itri1].G2[ied1] = (int)barArrayId;
                                        tris[(int)itri1].S2[ied1] = ibar;
                                        tris[(int)itri1].R2[ied1] = 1;
                                    }
                                }
                                {   // 内側の要素を辺にくっつける
                                    tris[(int)iTri0].G2[iEd0] = (int)barArrayId;
                                    tris[(int)iTri0].S2[iEd0] = ibar;
                                    tris[(int)iTri0].R2[iEd0] = 0;
                                }
                            }
                        }
                        if (barArray.LRId[1] == newTriId)
                        {
                            // 辺の右側を切り離す
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                MeshBar bar = bars[(int)ibar];
                                uint iTri0 = bar.S2[1];
                                uint iEd0 = bar.R2[1];
                                if (tris[(int)iTri0].G2[iEd0] == barArrayId)
                                {
                                    continue; // すでに切り離されてる
                                }
                                {
                                    // 外側の要素を切り離す
                                    uint itri1 = tris[(int)iTri0].S2[iEd0];
                                    uint ied1 = MeshUtils2D.RelTriTri[tris[(int)iTri0].R2[iEd0]][iEd0];
                                    System.Diagnostics.Debug.Assert(itri1 < tris.Count);
                                    System.Diagnostics.Debug.Assert(ied1 < 3);
                                    System.Diagnostics.Debug.Assert(tris[(int)iTri0].V[MeshUtils2D.TriElEdgeNo[iEd0][0]] ==
                                        vec2Pt[(int)bar.V[1]]);
                                    System.Diagnostics.Debug.Assert(tris[(int)iTri0].V[MeshUtils2D.TriElEdgeNo[iEd0][1]] ==
                                        vec2Pt[(int)bar.V[0]]);
                                    if (barArray.LRId[0] != newTriId)
                                    {
                                        // 外側の要素を切り離す
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == iTri0);
                                        tris[(int)itri1].G2[ied1] = -1;
                                    }
                                    else
                                    {
                                        // 辺をはさんで向かい側の要素も内側だから辺にくっつける
                                        tris[(int)itri1].G2[ied1] = (int)barArrayId;
                                        tris[(int)itri1].S2[ied1] = ibar;
                                        tris[(int)itri1].R2[ied1] = 0;
                                    }
                                }
                                {
                                    // 内側の要素を辺にくっつける
                                    tris[(int)iTri0].G2[iEd0] = (int)barArrayId;
                                    tris[(int)iTri0].S2[iEd0] = ibar;
                                    tris[(int)iTri0].R2[iEd0] = 1;
                                }
                            }
                        }
                    }
                    if (!loopItr.ShiftChildLoop())
                    {
                        break;
                    }
                }   // ループのfor文終わり

                // ここから先はFlip禁止フラグ(隣接要素配列番号-3)はないはず
                System.Diagnostics.Debug.Assert(MeshUtils2D.CheckTri(points, tris));
            }

            // 外側の三角形の消去
            ////////////////////////////////////////////////

            IList<MeshTri2D> inTris = new List<MeshTri2D>();    // 内側の三角形
            {
                // 外側の三角形の除去
                // 内側にある三角形をひとつ(iKerTri0)見つける
                uint iKerTri0 = (uint)tris.Count;
                {
                    LoopEdgeItr loopItr = cad.GetLoopEdgeItr(lId);
                    for (; !loopItr.IsEnd(); loopItr.Next())
                    {
                        uint eId;
                        bool isSameDir;
                        loopItr.GetEdgeId(out eId, out isSameDir);
                        uint loc;
                        uint type;
                        if (!FindElemLocTypeFromCadIdType(CadElementType.Edge, eId, out loc, out type))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(type == 1);
                        System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                        MeshBarArray2D barArray = BarArrays[(int)loc];
                        System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                        IList<MeshBar> bars = barArray.Bars;
                        if (barArray.LRId[0] == newTriId)
                        {
                            if (bars.Count > 0)
                            {
                                iKerTri0 = bars[0].S2[0];
                            }
                        }
                        else if (barArray.LRId[1] == newTriId)
                        {
                            if (bars.Count > 0)
                            {
                                iKerTri0 = bars[0].S2[1];
                            }
                        }
                    }
                }
                System.Diagnostics.Debug.Assert(iKerTri0 < tris.Count);

                // 領域の外の要素ならフラグが-1、そうでなければフラグは昇順の要素番号が入った配列inoutFlgsを作る
                uint inTriCnt;
                // フラグ配列
                IList<int> inoutFlgs = new List<int>();
                {   // 上で見つけた内側の三角形を核として内側の三角形を周囲に拡大していく
                    for (int i = 0; i < tris.Count; i++)
                    {
                        inoutFlgs.Add(-1);
                    }
                    inoutFlgs[(int)iKerTri0] = 0;
                    inTriCnt = 1;
                    // 周囲が探索されていない三角形
                    Stack<uint> indStack = new Stack<uint>();
                    indStack.Push(iKerTri0);
                    while (true)
                    {
                        if (indStack.Count == 0)
                        {
                            break;
                        }
                        uint iCurTri = indStack.Pop();
                        for (uint iTriNo = 0; iTriNo < 3; iTriNo++)
                        {
                            if (tris[(int)iCurTri].G2[iTriNo] != -2)
                            {
                                continue;
                            }
                            uint iSTri = tris[(int)iCurTri].S2[iTriNo];
                            if (inoutFlgs[(int)iSTri] == -1)
                            {
                                inoutFlgs[(int)iSTri] = (int)inTriCnt;
                                inTriCnt++;
                                indStack.Push(iSTri);
                            }
                        }
                    }
                }

                // フラグ配列に沿って内側の三角形を集めた配列inTriを作る
                for (int i = 0; i < inTriCnt; i++)
                {
                    inTris.Add(new MeshTri2D());
                }
                for (uint iTri = 0; iTri < tris.Count; iTri++)
                {
                    if (inoutFlgs[(int)iTri] != -1)
                    {
                        int iInTri = inoutFlgs[(int)iTri];
                        System.Diagnostics.Debug.Assert(iInTri >= 0 && iInTri < inTriCnt);
                        inTris[iInTri] = tris[(int)iTri];
                    }
                }
                // 内側の三角形配列のの隣接情報を作る
                for (uint iTri = 0; iTri < inTris.Count; iTri++)
                {
                    for (uint iFaTri = 0; iFaTri < 3; iFaTri++)
                    {
                        if (inTris[(int)iTri].G2[iFaTri] != -2)
                        {
                            continue;
                        }
                        int iSTri0 = (int)inTris[(int)iTri].S2[iFaTri];
                        System.Diagnostics.Debug.Assert(iSTri0 >= 0 && iSTri0 < tris.Count);
                        int iSInTri0 = inoutFlgs[iSTri0];
                        System.Diagnostics.Debug.Assert(iSInTri0 >= 0 && iSInTri0 < inTris.Count);
                        inTris[(int)iTri].S2[iFaTri] = (uint)iSInTri0;
                    }
                }
                { 
                    // 辺の隣接情報を更新
                    LoopEdgeItr loopItr = cad.GetLoopEdgeItr(lId);
                    while (true)
                    {
                        // 子ループのためのループ
                        for (; !loopItr.IsEnd(); loopItr.Next())
                        {
                            uint eId;
                            bool isSameDir;
                            if (!loopItr.GetEdgeId(out eId, out isSameDir))
                            {
                                continue;  // ループの中の点
                            }
                            uint loc;
                            uint type;
                            if (!FindElemLocTypeFromCadIdType(CadElementType.Edge, eId, out loc, out type))
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                            System.Diagnostics.Debug.Assert(type == 1);
                            MeshBarArray2D barArray = BarArrays[(int)loc];
                            System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                            IList<MeshBar> bars = barArray.Bars;
                            int barArrayId = (int)barArray.Id;
                            System.Diagnostics.Debug.Assert(barArrayId != newTriId);
                            uint iside = (isSameDir) ? 0 : 1u;
                            System.Diagnostics.Debug.Assert(barArray.LRId[iside] == newTriId);
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                MeshBar bar = bars[(int)ibar];
                                int iSTri0 = (int)bar.S2[(int)iside];
                                System.Diagnostics.Debug.Assert(iSTri0 >= 0 && iSTri0 < tris.Count);
                                int iSInTri0 = inoutFlgs[iSTri0];
                                System.Diagnostics.Debug.Assert(iSInTri0 >= 0 && iSInTri0 < inTris.Count);
                                bar.S2[iside] = (uint)iSInTri0;
                            }
                        }
                        if (!loopItr.ShiftChildLoop())
                        {
                            break;
                        }
                    }
                }
                inoutFlgs.Clear();
                for (uint iPt = 0; iPt < points.Count; iPt++)
                {
                    points[(int)iPt].Element = -1;
                }
                System.Diagnostics.Debug.Assert(MeshUtils2D.CheckTri(points, inTris));
            }
            {
                // Remove not used point
                IList<int> pt2Vec = new List<int>();
                for (int i = 0; i < points.Count; i++)
                {
                    pt2Vec.Add(-2);
                }
                for (uint iTri = 0; iTri < inTris.Count; iTri++)
                {
                    pt2Vec[(int)inTris[(int)iTri].V[0]] = -1;
                    pt2Vec[(int)inTris[(int)iTri].V[1]] = -1;
                    pt2Vec[(int)inTris[(int)iTri].V[2]] = -1;
                }
                for (uint iVec = 0; iVec < vec2Pt.Count; iVec++)
                {
                    if (vec2Pt[(int)iVec] != -1)
                    {
                        uint iPt0 = (uint)vec2Pt[(int)iVec];
                        if (pt2Vec[(int)iPt0] != -1)
                        {
                            System.Diagnostics.Debug.WriteLine("対応しない点");
                            return false;
                        }
                        System.Diagnostics.Debug.Assert(pt2Vec[(int)iPt0] == -1);
                        pt2Vec[(int)iPt0] = (int)iVec;
                    }
                }
                for (uint iPt = 0; iPt < pt2Vec.Count; iPt++)
                {
                    if (pt2Vec[(int)iPt] == -1)
                    {
                        System.Diagnostics.Debug.WriteLine(iPt + " (" + points[(int)iPt].Point.X +
                            " " + points[(int)iPt].Point.Y);
                        //"未実装  Ｌｏｏｐに新しい節点の追加したときの処理"
                        return false;
                    }
                }
                for (uint iTri = 0; iTri < inTris.Count; iTri++)
                {
                    for (uint iTriNo = 0; iTriNo < 3; iTriNo++)
                    {
                        int iPt0 = (int)inTris[(int)iTri].V[iTriNo];
                        System.Diagnostics.Debug.Assert(iPt0 >= 0 && iPt0 < points.Count);
                        int iVec0 = pt2Vec[iPt0];
                        System.Diagnostics.Debug.Assert(iVec0 >= 0 && iVec0 < Vecs.Count);
                        inTris[(int)iTri].V[iTriNo] = (uint)iVec0;
                    }
                }
            }

            {
                uint iTriArray = (uint)TriArrays.Count;
                TriArrays.Add(new MeshTriArray2D());
                TriArrays[(int)iTriArray].Tris = inTris;
                TriArrays[(int)iTriArray].LCadId = lId;
                TriArrays[(int)iTriArray].Id = newTriId;
                TriArrays[(int)iTriArray].Layer = cad.GetLayer(CadElementType.Loop, lId);

                int typeLocCnt = TypeLocs.Count;
                for (int i = typeLocCnt; i < newTriId + 1; i++)
                {
                    var typeLoc = new MeshTypeLoc { Type = 0, Loc = -1 };
                    TypeLocs.Add(typeLoc);
                }
                TypeLocs[(int)newTriId].Type = 2;   // TRI
                TypeLocs[(int)newTriId].Loc = (int)iTriArray;
            }

            System.Diagnostics.Debug.Assert(CheckMesh() == 0);
            return true;
        }

        private void MakeIncludeRelation(Cad2D cad)
        {
            if (TypeLocs.Count == 0)
            {
                return;
            }

            System.Diagnostics.Debug.Assert(FindMaxId() + 1 == TypeLocs.Count);
            foreach (var typeLoc in TypeLocs)
            {
                typeLoc.IncludeRelations.Clear();
            }

            IList<uint> lIds = cad.GetElementIds(CadElementType.Loop);
            for (uint iLId = 0; iLId < lIds.Count; iLId++)
            {
                uint lId = lIds[(int)iLId];
                uint triId = GetIdFromCadId(lId, CadElementType.Loop);
                if (!IsId(triId))
                {
                    continue;
                }
                LoopEdgeItr lItr = cad.GetLoopEdgeItr(lId);
                while (true)
                {
                    for (; !lItr.IsEnd(); lItr.Next())
                    {
                        uint vCadId = lItr.GetVertexId();
                        uint meshVId = GetIdFromCadId(vCadId, CadElementType.Vertex);
                        TypeLocs[(int)triId].IncludeRelations.Add(meshVId);
                        System.Diagnostics.Debug.Assert(IsId(meshVId));

                        uint eId;
                        bool isSameDir;
                        if (!lItr.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;
                        }
                        uint barId = GetIdFromCadId(eId, CadElementType.Edge);
                        System.Diagnostics.Debug.Assert(IsId(barId));
                        TypeLocs[(int)triId].IncludeRelations.Add(barId);
                    }
                    if (!lItr.ShiftChildLoop())
                    {
                        break;
                    }
                }
            }

            IList<uint> eIds = cad.GetElementIds(CadElementType.Edge);
            for (uint iEId = 0; iEId < eIds.Count; iEId++)
            {
                uint eId = eIds[(int)iEId];
                System.Diagnostics.Debug.Assert(cad.IsElementId(CadElementType.Edge, eId));
                uint barId = GetIdFromCadId(eId, CadElementType.Edge);
                if (!IsId(barId))
                {
                    // 浮いている辺があって，辺メッシュが切られなかった場合
                    continue;
                }
                uint sVId;
                uint eVId;
                if (!cad.GetEdgeVertexId(eId, out sVId, out eVId))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint meshSVId = GetIdFromCadId(sVId, CadElementType.Vertex);
                uint meshEVId = GetIdFromCadId(eVId, CadElementType.Vertex);
                System.Diagnostics.Debug.Assert(IsId(meshSVId));
                System.Diagnostics.Debug.Assert(IsId(meshEVId));
                TypeLocs[(int)barId].IncludeRelations.Add(meshSVId);
                TypeLocs[(int)barId].IncludeRelations.Add(meshEVId);
            }
        }

        private int CheckMesh()
        {
            {
                uint maxId = 0;
                for (uint iVer = 0; iVer < Vertexs.Count; iVer++)
                {
                    uint id0 = Vertexs[(int)iVer].Id;
                    if (maxId < id0)
                    {
                        maxId = id0;
                    }
                    System.Diagnostics.Debug.Assert(TypeLocs.Count > id0);
                    System.Diagnostics.Debug.Assert(TypeLocs[(int)id0].Type == 0); // VERTEX
                    int loc0 = TypeLocs[(int)id0].Loc;
                    System.Diagnostics.Debug.Assert(loc0 == (int)iVer);
                    System.Diagnostics.Debug.Assert(Vertexs.Count > loc0);
                    MeshVertex2D ver0 = Vertexs[(int)loc0];
                    System.Diagnostics.Debug.Assert(ver0.Id == id0);
                }
                for (uint iBarArray = 0; iBarArray < BarArrays.Count; iBarArray++)
                {
                    uint id0 = BarArrays[(int)iBarArray].Id;
                    if (maxId < id0)
                    {
                        maxId = id0;
                    }
                    System.Diagnostics.Debug.Assert(TypeLocs.Count > id0);
                    System.Diagnostics.Debug.Assert(TypeLocs[(int)id0].Type == 1); // BAR
                    int loc0 = TypeLocs[(int)id0].Loc;
                    System.Diagnostics.Debug.Assert(loc0 == (int)iBarArray);
                    System.Diagnostics.Debug.Assert(BarArrays.Count > loc0);
                    MeshBarArray2D bar0 = BarArrays[(int)loc0];
                    System.Diagnostics.Debug.Assert(bar0.Id == id0);
                }
                for (uint iTriArray = 0; iTriArray< TriArrays.Count; iTriArray++)
                {
                    uint id0 = TriArrays[(int)iTriArray].Id;
                    if (maxId < id0)
                    {
                        maxId = id0;
                    }
                    System.Diagnostics.Debug.Assert(TypeLocs.Count > id0);
                    System.Diagnostics.Debug.Assert(TypeLocs[(int)id0].Type == 2); // TRI
                    int loc0 = TypeLocs[(int)id0].Loc;
                    System.Diagnostics.Debug.Assert(loc0 == (int)iTriArray);
                    System.Diagnostics.Debug.Assert(TriArrays.Count > loc0);
                    MeshTriArray2D tri0 = TriArrays[(int)loc0];
                    System.Diagnostics.Debug.Assert(tri0.Id == id0);
                }
                System.Diagnostics.Debug.Assert(maxId == FindMaxId());
                uint[] isUsedFlgs = new uint[maxId + 1];
                for (uint iVer = 0; iVer < Vertexs.Count; iVer++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)Vertexs[(int)iVer].Id] == 0);
                    isUsedFlgs[(int)Vertexs[(int)iVer].Id] = 1;
                }
                for (uint iBarArray = 0; iBarArray< BarArrays.Count; iBarArray++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)BarArrays[(int)iBarArray].Id] == 0);
                    isUsedFlgs[(int)BarArrays[(int)iBarArray].Id] = 1;
                }
                for (uint iTriArray = 0; iTriArray < TriArrays.Count; iTriArray++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)TriArrays[(int)iTriArray].Id] == 0);
                    isUsedFlgs[(int)TriArrays[(int)iTriArray].Id] = 1;
                }
                System.Diagnostics.Debug.Assert(isUsedFlgs[0] == 0 );
            }
            for (uint iVer = 0; iVer < Vertexs.Count; iVer++)
            {
                System.Diagnostics.Debug.Assert(IsId(Vertexs[(int)iVer].Id));
            }
            for (uint iBarArray = 0; iBarArray< BarArrays.Count;iBarArray++)
            {
                System.Diagnostics.Debug.Assert(IsId(BarArrays[(int)iBarArray].Id));
            }
            for (uint iTriArray = 0; iTriArray< TriArrays.Count; iTriArray++)
            {
                System.Diagnostics.Debug.Assert(IsId(TriArrays[(int)iTriArray].Id));
            }
            for (uint index = 0; index < TypeLocs.Count; index++)
            {
                if (TypeLocs[(int)index].Loc == -1)
                {
                    continue;
                }
                System.Diagnostics.Debug.Assert(TypeLocs[(int)index].Loc >= 0);
                System.Diagnostics.Debug.Assert(IsId(index));
            }
            ////////////////////////////////	
            for (uint iBarArray = 0; iBarArray < BarArrays.Count; iBarArray++)
            {
                uint barMeshId = BarArrays[(int)iBarArray].Id;
                int barLoc = TypeLocs[(int)barMeshId].Loc;
                IList<MeshBar> bars = BarArrays[barLoc].Bars;
                for (uint iSideBar = 0; iSideBar < 2; iSideBar++)
                {
                    int meshAdjId = (int)BarArrays[barLoc].LRId[iSideBar];
                    if (meshAdjId <= 0)
                    {
                        continue;  // 外部と接している場合
                    }
                    System.Diagnostics.Debug.Assert(meshAdjId < TypeLocs.Count);
                    int adjLoc = TypeLocs[meshAdjId].Loc;
                    if (TypeLocs[meshAdjId].Type == 2)
                    {
                        // 三角形と接している場合
                        IList<MeshTri2D> tris = TriArrays[adjLoc].Tris;
                        for (uint ibar = 0; ibar < bars.Count; ibar++)
                        {
                            uint itri = bars[(int)ibar].S2[iSideBar];
                            uint inotri = bars[(int)ibar].R2[iSideBar];
                            System.Diagnostics.Debug.Assert(tris[(int)itri].G2[inotri] == (int)barMeshId);
                            System.Diagnostics.Debug.Assert(tris[(int)itri].S2[inotri] == ibar);
                            System.Diagnostics.Debug.Assert(tris[(int)itri].R2[inotri] == iSideBar);
                        }
                    }
                }
            }
            return 0;
        }

        private bool MakeMeshElemLength(
            Cad2D cad,
            IList<uint> loopIds, IList<double> loopELens, IList<uint> edgeIds, IList<double> edgeELens)
        {
            System.Diagnostics.Debug.Assert(loopIds.Count == loopELens.Count);
            System.Diagnostics.Debug.Assert(edgeIds.Count == edgeELens.Count);

            ClearMeshData();

            {
                // ループに使われている頂点
                IList<uint> vtxFlgs = new List<uint>();
                for (int iLId = 0; iLId < loopIds.Count; iLId++)
                {
                    uint lId = loopIds[iLId];
                    LoopEdgeItr lItr = cad.GetLoopEdgeItr(lId);
                    while (true)
                    {
                        for (; !lItr.IsEnd(); lItr.Next())
                        {
                            uint vId = lItr.GetVertexId();
                            if (vtxFlgs.Count <= vId)
                            {
                                int cnt = vtxFlgs.Count;
                                for (int iTmp = cnt; iTmp < vId + 1; iTmp++)
                                {
                                    vtxFlgs.Add(0);
                                }
                            }
                            vtxFlgs[(int)vId] = 1;
                        }
                        if (!lItr.ShiftChildLoop())
                        {
                            break;
                        }
                    }
                }
                // 辺に使われている頂点
                for (int iEId = 0; iEId < edgeIds.Count; iEId++)
                {
                    uint eId = edgeIds[iEId];
                    uint sVId;
                    uint eVId;
                    cad.GetEdgeVertexId(eId, out sVId, out eVId);
                    uint[] vIds = { sVId, eVId };
                    foreach (uint vId in vIds)
                    {
                        if (vtxFlgs.Count <= vId)
                        {
                            int cnt = vtxFlgs.Count;
                            for (int iTmp = cnt; iTmp < vId + 1; iTmp++)
                            {
                                vtxFlgs.Add(0);
                            }
                        }
                        vtxFlgs[(int)vId] = 1;
                    }
                }
                // フラグを立てた頂点のメッシュオブジェクトを生成する
                for (uint vId = 0; vId < vtxFlgs.Count; vId++)
                {
                    if (vtxFlgs[(int)vId] == 0)
                    {
                        continue;
                    }
                    uint addId = GetFreeObjectId();
                    OpenTK.Vector2d vec = cad.GetVertexCoord(vId);
                    Vecs.Add(vec);
                    {
                        MeshVertex2D tmpVer = new MeshVertex2D();
                        tmpVer.Id = addId;
                        tmpVer.VCadId = vId;
                        tmpVer.Layer = cad.GetLayer(CadElementType.Vertex, vId);
                        tmpVer.V = (uint)(Vecs.Count - 1);

                        Vertexs.Add(tmpVer);

                    }
                    {
                        int typeLocCnt = TypeLocs.Count;
                        for (int iTmp = typeLocCnt; iTmp < addId + 1; iTmp++)
                        {
                            var typeLoc = new MeshTypeLoc { Type = 0, Loc = -1 };
                            TypeLocs.Add(typeLoc);
                        }
                        TypeLocs[(int)addId].Loc = Vertexs.Count - 1;
                        TypeLocs[(int)addId].Type = 0; // VERTEX
                    }
                }
                System.Diagnostics.Debug.Assert(CheckMesh() == 0);
            }

            // 辺を作る
            for (int iEId = 0; iEId < edgeIds.Count; iEId++)
            {
                uint eId = edgeIds[iEId];
                double eLen = edgeELens[iEId];
                if (GetIdFromCadId(eId, CadElementType.Edge) != 0)
                {
                    // 登録済み
                    continue;
                }
                MakeMeshEdge(cad, eId, eLen);
                System.Diagnostics.Debug.Assert(CheckMesh() == 0);
            }

            for (int iLId = 0; iLId < loopIds.Count; iLId++)
            {
                // ループに必要な辺を作る
                uint lId = loopIds[iLId];
                double eLen = loopELens[iLId];
                for (LoopEdgeItr lItr = cad.GetLoopEdgeItr(lId); !lItr.IsChildEnd; lItr.ShiftChildLoop())
                {
                    for (lItr.Begin(); !lItr.IsEnd(); lItr.Next())
                    {
                        uint eId;
                        bool isSameDir;
                        if (!lItr.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;
                        }
                        if (GetIdFromCadId(eId, CadElementType.Edge) != 0)
                        {
                            // 登録済み
                            continue;
                        }
                        MakeMeshEdge(cad, eId, eLen);
                        System.Diagnostics.Debug.Assert(CheckMesh() == 0);
                    }
                }
            }

            for (int iLId = 0; iLId < loopIds.Count; iLId++)
            { 
                // ループを作る
                uint lId = loopIds[iLId];
                double eLen = loopELens[iLId];
                System.Diagnostics.Debug.Assert(eLen > 0.0);
                MakeMeshLoop(cad, lId, eLen);

                System.Diagnostics.Debug.Assert(CheckMesh() == 0);
            }

            MakeIncludeRelation(cad);
            return true;
        }

        private bool MakeMeshEdge(Cad2D cad, uint eId, double len)
        {
            System.Diagnostics.Debug.Assert(GetIdFromCadId(eId, CadElementType.Edge) == 0);

            uint sVId;
            uint eVId;
            if (!cad.GetEdgeVertexId(eId, out sVId, out eVId))
            {
                System.Diagnostics.Debug.Assert(false);
            }

            // 始点、終点のメッシュ点番号をsPId,ePIdに代入
            uint sPId;
            uint ePId;
            uint sMeshId;
            uint eMeshId;
            {
                uint loc;
                uint type;
                if (!FindElemLocTypeFromCadIdType(CadElementType.Vertex, sVId, out loc, out type))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 0 && loc < Vertexs.Count);
                MeshVertex2D sVP = Vertexs[(int)loc];
                sPId = sVP.V;
                sMeshId = sVP.Id;
                if (!FindElemLocTypeFromCadIdType(CadElementType.Vertex, eVId, out loc, out type))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 0 && loc < Vertexs.Count);
                MeshVertex2D eVP = Vertexs[(int)loc];
                ePId = eVP.V;
                eMeshId = eVP.Id;
            }

            uint newElemId = GetFreeObjectId();
            uint iBarArray0 = (uint)BarArrays.Count;
            BarArrays.Add(new MeshBarArray2D());
            {
                int typeLocCnt = TypeLocs.Count;
                for (int i = typeLocCnt; i < newElemId + 1; i++)
                {
                    var typeLoc = new MeshTypeLoc { Type = 0, Loc = -1 };
                    TypeLocs.Add(typeLoc);
                }
                TypeLocs[(int)newElemId].Loc = (int)iBarArray0;
                TypeLocs[(int)newElemId].Type = 1; // BAR
            }
            MeshBarArray2D barArray = BarArrays[(int)iBarArray0];
            IList<OpenTK.Vector2d> pts;
            cad.GetCurveAsPolyline(eId, out pts, len);
            ////////////////
            uint div = (uint)pts.Count + 1;
            IList<uint> ptIds = new List<uint>();
            {
                for (int i = 0; i < div + 1; i++)
                {
                    ptIds.Add(0);
                }
                ptIds[0] = sPId;
                for (int i = 1; i < div; i++)
                {
                    ptIds[i] = (uint)Vecs.Count;
                    Vecs.Add(pts[i - 1]);
                }
                ptIds[(int)div] = ePId;
            }
            {
                barArray.Id = newElemId;
                barArray.ECadId = eId;
                barArray.Layer = cad.GetLayer(CadElementType.Edge, eId);
                barArray.SEId[0] = sMeshId;
                barArray.SEId[1] = eMeshId;
                barArray.LRId[0] = 0;
                barArray.LRId[1] = 0;
                barArray.Bars.Clear();
                for (int ibar = 0; ibar < div; ibar++)
                {
                    MeshBar bar = new MeshBar();
                    bar.V[0] = ptIds[ibar];
                    bar.V[1] = ptIds[ibar + 1];
                    bar.S2[0] = 0;
                    bar.S2[1] = 0;
                    bar.R2[0] = 0;
                    bar.R2[1] = 0;
                    barArray.Bars.Add(bar);
                }
            }
            System.Diagnostics.Debug.Assert(CheckMesh() == 0);
            return true;
        }

        private bool MakeMeshLoop(Cad2D cad, uint lId, double len)
        {
            if (!TessellateLoop(cad, lId))
            {
                System.Diagnostics.Debug.WriteLine("TessellateLoop Fail");
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            IList<MeshPoint2D> points = new List<MeshPoint2D>();
            IList<MeshTri2D> tris = new List<MeshTri2D>();
            // MSH節点番号vecからローカル節点番号poへのフラグ、対応してない場合は-2が入る
            IList<int> vec2Pt = new List<int>();
            {
                int vecCnt = Vecs.Count;
                for (int i = 0; i < vecCnt; i++)
                {
                    vec2Pt.Add(-2);
                }
                uint loc;
                uint type;
                if (!FindElemLocTypeFromCadIdType(CadElementType.Loop, lId, out loc, out type))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 2);
                System.Diagnostics.Debug.Assert(loc < TriArrays.Count);
                MeshTriArray2D triArray = TriArrays[(int)loc];
                IList<MeshTri2D> iniTris = new List<MeshTri2D>();
                for (int i = 0; i < triArray.Tris.Count; i++)
                {
                    iniTris.Add(new MeshTri2D(triArray.Tris[i]));
                }
                for (uint iTri = 0; iTri < iniTris.Count; iTri++)
                { 
                    // 三角形に使われている全ての節点をマーク
                    for (uint iTriNo = 0; iTriNo < 3; iTriNo++)
                    {
                        uint ivec0 = iniTris[(int)iTri].V[iTriNo];
                        System.Diagnostics.Debug.Assert(ivec0 < Vecs.Count);
                        vec2Pt[(int)ivec0] = -1;
                    }
                }
                uint ptCnt0 = 0;
                for (uint iVec = 0; iVec < Vecs.Count; iVec++)
                {
                    if (vec2Pt[(int)iVec] == -1)
                    {
                        vec2Pt[(int)iVec] = (int)ptCnt0;
                        ptCnt0++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(vec2Pt[(int)iVec] == -2);
                    }
                }
                int ptCnt1 = points.Count;
                for (int i = ptCnt1; i < ptCnt0; i++)
                {
                    points.Add(new MeshPoint2D());
                }
                for (uint iVec = 0; iVec < Vecs.Count; iVec++)
                {
                    if (vec2Pt[(int)iVec] >= 0)
                    {
                        int ipo0 = vec2Pt[(int)iVec];
                        System.Diagnostics.Debug.Assert(ipo0 >= 0 && ipo0 < points.Count);
                        points[ipo0].Point = new OpenTK.Vector2d(Vecs[(int)iVec].X, Vecs[(int)iVec].Y);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(vec2Pt[(int)iVec] == -2);
                    }
                }
                tris.Clear();
                for (int i = 0; i < iniTris.Count; i++)
                {
                    tris.Add(new MeshTri2D(iniTris[i]));
                }
                for (uint iTri = 0; iTri < iniTris.Count; iTri++)
                {
                    for (uint iTriNo = 0; iTriNo < 3; iTriNo++)
                    {
                        uint ivec0 = iniTris[(int)iTri].V[iTriNo];
                        System.Diagnostics.Debug.Assert(ivec0 < Vecs.Count);
                        int ipo0 = vec2Pt[(int)ivec0];
                        System.Diagnostics.Debug.Assert(ipo0 >= 0 && ipo0 < (int)points.Count);
                        System.Diagnostics.Debug.Assert(tris[(int)iTri].V[iTriNo] ==
                            iniTris[(int)iTri].V[iTriNo]);
                        tris[(int)iTri].V[iTriNo] = (uint)ipo0;
                    }
                }
                for (uint iTri = 0; iTri < tris.Count; iTri++)
                {
                    for (uint iTriNo = 0; iTriNo < 3; iTriNo++)
                    {
                        uint ipo0 = tris[(int)iTri].V[iTriNo];
                        System.Diagnostics.Debug.Assert(ipo0 < points.Count);
                        points[(int)ipo0].Element = (int)iTri;
                        points[(int)ipo0].Node = iTriNo;
                    }
                }
                System.Diagnostics.Debug.Assert(MeshUtils2D.CheckTri(points, tris));
            }

            // フラグが１なら動かさない
            IList<uint> isntMoves = new List<uint>();
            {
                int ptCnt2 = points.Count;
                for (int i = 0; i < ptCnt2; i++)
                {
                    isntMoves.Add(0);
                }
                for (uint iVer = 0; iVer < Vertexs.Count; iVer++)
                {
                    uint ivec = Vertexs[(int)iVer].V;
                    if (ivec < vec2Pt.Count)
                    {
                        if (vec2Pt[(int)ivec] == -2)
                        {
                            continue;
                        }
                        System.Diagnostics.Debug.Assert(vec2Pt[(int)ivec] >= 0);
                        uint ipo = (uint)vec2Pt[(int)ivec];
                        if (ipo < points.Count)
                        {
                            isntMoves[(int)ipo] = 1;
                        }
                    }
                }
            }

            {
                // trisに節点を追加
                double ratio = 3.0;
                while (true)
                {
                    uint nadd = 0;
                    for (uint iTri = 0; iTri < tris.Count; iTri++)
                    {
                        double area = CadUtils2D.TriArea(
                            points[(int)tris[(int)iTri].V[0]].Point,
                            points[(int)tris[(int)iTri].V[1]].Point,
                            points[(int)tris[(int)iTri].V[2]].Point);
                        if (area > len * len * ratio)
                        {
                            // itriの重心に新しい節点を追加
                            uint iPt0 = (uint)points.Count; // iPt0は新しい節点番号
                            int ptnCnt3 = points.Count;
                            for (int iTmp = ptnCnt3; iTmp < ptnCnt3 + 1; iTmp++)
                            {
                                points.Add(new MeshPoint2D());
                            }
                            points[(int)iPt0].Point = new OpenTK.Vector2d(
                                ((points[(int)tris[(int)iTri].V[0]].Point.X +
                                points[(int)tris[(int)iTri].V[1]].Point.X +
                                points[(int)tris[(int)iTri].V[2]].Point.X) / 3.0),
                                ((points[(int)tris[(int)iTri].V[0]].Point.Y +
                                points[(int)tris[(int)iTri].V[1]].Point.Y +
                                points[(int)tris[(int)iTri].V[2]].Point.Y) / 3.0));
                            MeshUtils2D.InsertPointElem(iPt0, iTri, points, tris);
                            MeshUtils2D.DelaunayAroundPoint(iPt0, points, tris);
                            nadd++;
                        }
                    }
                    MeshUtils2D.LaplacianSmoothing(points, tris, isntMoves);
                    if (nadd != 0)
                    {
                        ratio *= 0.8;
                    }
                    else
                    {
                        ratio *= 0.5;
                    }
                    if (ratio < 0.65)
                    {
                        break;
                    }
                }
            }

            MeshUtils2D.LaplaceDelaunaySmoothing(points, tris, isntMoves);

            // 全体節点番号へ直す
            IList<int> pt2Vec = new List<int>();
            int ptCnt = points.Count;
            for (int i = 0; i < ptCnt; i++)
            {
                pt2Vec.Add(-2);
            }
            for (uint iTri = 0; iTri < tris.Count; iTri++)
            {
                pt2Vec[(int)tris[(int)iTri].V[0]] = -1;
                pt2Vec[(int)tris[(int)iTri].V[1]] = -1;
                pt2Vec[(int)tris[(int)iTri].V[2]] = -1;
            }
            for (uint iVec = 0; iVec < vec2Pt.Count; iVec++)
            {
                if (vec2Pt[(int)iVec] >= 0)
                {
                    uint iPt0 = (uint)vec2Pt[(int)iVec];
                    System.Diagnostics.Debug.Assert(pt2Vec[(int)iPt0] == -1);
                    pt2Vec[(int)iPt0] = (int)iVec;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(vec2Pt[(int)iVec] == -2);
                }
            }
            {  
                // 全体節点を追加
                uint addPtCnt = 0;
                for (uint iPt = 0; iPt < pt2Vec.Count; iPt++)
                {
                    if (pt2Vec[(int)iPt] == -1)
                    {
                        addPtCnt++;
                    }
                }
                for (uint iPt = 0; iPt < pt2Vec.Count; iPt++)
                {
                    if (pt2Vec[(int)iPt] == -1)
                    {
                        OpenTK.Vector2d vec0 = new OpenTK.Vector2d(
                            points[(int)iPt].Point.X, points[(int)iPt].Point.Y);
                        uint iVec0 = (uint)Vecs.Count;
                        Vecs.Add(vec0);
                        pt2Vec[(int)iPt] = (int)iVec0;
                    }
                }
            }
            { 
                // ローカル節点番号から全体節点番号への並び替え
                for (uint itri = 0; itri < tris.Count; itri++)
                {
                    for (uint inotri = 0; inotri < 3; inotri++)
                    {
                        int iPt0 = (int)tris[(int)itri].V[inotri];
                        System.Diagnostics.Debug.Assert(iPt0 >= 0 && iPt0 < points.Count);
                        uint iVec0 = (uint)pt2Vec[iPt0];
                        System.Diagnostics.Debug.Assert(iVec0 < Vecs.Count);
                        tris[(int)itri].V[inotri] = iVec0;
                    }
                }
            }

            uint thisLoopId;
            {
                uint loc;
                uint type;
                if (!FindElemLocTypeFromCadIdType(CadElementType.Loop, lId, out loc, out type))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 2);
                System.Diagnostics.Debug.Assert(loc < TriArrays.Count);
                MeshTriArray2D triArray = TriArrays[(int)loc];
                thisLoopId = triArray.Id;
            }

            {
                // 境界における要素との整合性をとる
                for (uint iTri = 0; iTri < tris.Count; iTri++)
                {
                    for (uint iFaTri = 0; iFaTri < 3; iFaTri++)
                    {
                        if (tris[(int)iTri].G2[iFaTri] < 0)
                        {
                            continue;
                        }
                        uint id0 = (uint)tris[(int)iTri].G2[iFaTri];
                        uint ele0 = tris[(int)iTri].S2[iFaTri];
                        System.Diagnostics.Debug.Assert(id0 < TypeLocs.Count);
                        uint type0 = (uint)TypeLocs[(int)id0].Type;
                        int loc0 = TypeLocs[(int)id0].Loc;
                        if (type0 == 1)
                        {
                            System.Diagnostics.Debug.Assert(loc0 < BarArrays.Count);
                            MeshBarArray2D barArray = BarArrays[loc0];
                            System.Diagnostics.Debug.Assert(barArray.Id == id0);
                            System.Diagnostics.Debug.Assert(ele0 < barArray.Bars.Count);
                            MeshBar bar = barArray.Bars[(int)ele0];
                            uint iVer0 = tris[(int)iTri].V[MeshUtils2D.TriElEdgeNo[iFaTri][0]];
                            uint iVer1 = tris[(int)iTri].V[MeshUtils2D.TriElEdgeNo[iFaTri][1]];
                            if (iVer0 == bar.V[0] && iVer1 == bar.V[1])
                            {
                                System.Diagnostics.Debug.Assert(barArray.LRId[0] == thisLoopId);
                                bar.S2[0] = iTri;
                                bar.R2[0] = iFaTri;
                                tris[(int)iTri].R2[iFaTri] = 0;
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(iVer0 == bar.V[1] && iVer1 == bar.V[0]);
                                System.Diagnostics.Debug.Assert(barArray.LRId[1] == thisLoopId);
                                bar.S2[1] = iTri;
                                bar.R2[1] = iFaTri;
                                tris[(int)iTri].R2[iFaTri] = 1;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Error!-->Not defined type" + type0);
                            System.Diagnostics.Debug.Assert(false);
                        }
                    }
                }
            }

            {
                uint loc;
                uint type;
                if (!FindElemLocTypeFromCadIdType(CadElementType.Loop, lId, out loc, out type))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 2);
                System.Diagnostics.Debug.Assert(loc < TriArrays.Count);
                System.Diagnostics.Debug.Assert(TriArrays[(int)loc].LCadId == lId);
                TriArrays[(int)loc].Tris = tris;
            }

            return true;
        }

        public bool Serialize(Serializer arch, bool isOnlyCadMshLink = false)
        {
            if (arch.IsLoading)
            {
                // 読み込み時の処理
                Clear();

                string className;
                string[] values;

                className = arch.ReadDepthClassName();
                System.Diagnostics.Debug.Assert(className == "CMesher2D");
                arch.ShiftDepth(true);

                ////////////////////////////////
                // CADとの接続関係のロード

                IList<uint> loopCadIds = new List<uint>();
                IList<double> eLens = new List<double>();
                {
                    className = arch.ReadDepthClassName();
                    System.Diagnostics.Debug.Assert(className == "setIdLCad_CutMesh");
                    int nl;
                    values = arch.GetValues();
                    nl = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(nl >= 0);
                    uint ind;
                    uint lId;
                    for (uint il = 0; il < nl; il++)
                    {
                        values = arch.GetValues();
                        ind = uint.Parse(values[0]);
                        lId = uint.Parse(values[1]);
                        System.Diagnostics.Debug.Assert(ind == il);
                        loopCadIds.Add(lId);
                    }
                }
                {
                    // メッシュ生成モードのロード
                    int ntmp0;
                    int ntmp1;
                    double dtmp0;
                    values = arch.GetValues();
                    ntmp0 = int.Parse(values[0]);
                    ntmp1 = int.Parse(values[1]);
                    dtmp0 = double.Parse(values[2]);
                    System.Diagnostics.Debug.Assert(ntmp0 >= 0 && ntmp0 < 3);
                    MeshingMode = (uint)ntmp0;
                    System.Diagnostics.Debug.Assert(ntmp1 > 0);
                    uint eSize = (uint)ntmp1;
                    System.Diagnostics.Debug.Assert(dtmp0 > 0);
                    double eLen0 = dtmp0;
                    if (eLen0 >= 0.0)
                    {
                        // 単一メッシュの形式
                        eLens.Clear();
                        for (int i = 0; i < loopCadIds.Count; i++)
                        {
                            eLens.Add(eLen0);
                        }
                    }
                    else
                    {
                        // eLen0 == -1のとき
                        values = arch.GetValues();
                        System.Diagnostics.Debug.Assert(values.Length == loopCadIds.Count);
                        eLens.Clear();
                        for (int i = 0; i < values.Length; i++)
                        {
                            double eLen = double.Parse(values[i]);
                            eLens.Add(eLen);
                        }
                    }
                }
                MeshingLoopCadIdELens.Clear();
                for (int i = 0; i < loopCadIds.Count; i++)
                {
                    var cadIdELen = new CadIdELen { CadId = loopCadIds[i], ELen = eLens[i] };
                    MeshingLoopCadIdELens.Add(cadIdELen);
                }

                if (isOnlyCadMshLink)
                {
                    arch.ShiftDepth(false);
                    return true;
                }

                ////////////////////////////////
                // メッシュ情報のロード

                {   // 座標をロード
                    int nvec;
                    int ndim;
                    values = arch.GetValues();
                    nvec = int.Parse(values[0]);
                    ndim = int.Parse(values[1]);
                    System.Diagnostics.Debug.Assert(nvec > 0 && (ndim > 0 && ndim < 4));
                    Vecs.Clear();
                    for (int ivec = 0; ivec < nvec; ivec++)
                    {
                        int itmp0;
                        double x;
                        double y;
                        values = arch.GetValues();
                        itmp0 = int.Parse(values[0]);
                        x = double.Parse(values[1]);
                        y = double.Parse(values[2]);
                        System.Diagnostics.Debug.Assert(itmp0 == ivec);
                        OpenTK.Vector2d vec = new OpenTK.Vector2d(x, y);
                        Vecs.Add(vec);
                    }
                }
                int nVerA;
                int nBarA;
                int nTriA;
                int nQuadA;
                values = arch.GetValues();
                nVerA = int.Parse(values[0]);
                nBarA = int.Parse(values[1]);
                nTriA = int.Parse(values[2]);
                nQuadA = int.Parse(values[3]);
                System.Diagnostics.Debug.Assert(nVerA >= 0 && nBarA >= 0 && nTriA >= 0 && nQuadA >= 0);
                for (int iVerA = 0; iVerA < nVerA; iVerA++)
                {
                    className = arch.ReadDepthClassName();
                    System.Diagnostics.Debug.Assert(className == "SVertex");
                    int id;
                    values = arch.GetValues();
                    id = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(id > 0);
                    int cadId;
                    values = arch.GetValues();
                    cadId = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(cadId >= 0);
                    int iv;
                    values = arch.GetValues();
                    iv = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(iv >= 0);
                    MeshVertex2D ver = new MeshVertex2D();
                    ver.Id = (uint)id;
                    ver.VCadId = (uint)cadId;
                    ver.V = (uint)iv;
                    Vertexs.Add(ver);
                }
                for (int iBarA = 0; iBarA < nBarA; iBarA++)
                {
                    className = arch.ReadDepthClassName();
                    System.Diagnostics.Debug.Assert(className == "CBarAry");
                    int id;
                    values = arch.GetValues();
                    id = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(id > 0);
                    int cadId;
                    values = arch.GetValues();
                    cadId = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(cadId >= 0);
                    int sId;
                    int eId;
                    values = arch.GetValues();
                    sId = int.Parse(values[0]);
                    eId = int.Parse(values[1]);
                    int lId;
                    int rId;
                    values = arch.GetValues();
                    lId = int.Parse(values[0]);
                    rId = int.Parse(values[1]);
                    int nbar;
                    values = arch.GetValues();
                    nbar = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(nbar > 0);
                    MeshBarArray2D barA = new MeshBarArray2D();
                    BarArrays.Add(barA);
                    barA.Id = (uint)id;
                    barA.ECadId = (uint)cadId;
                    barA.SEId[0] = (uint)sId;
                    barA.SEId[1] = (uint)eId;
                    barA.LRId[0] = (uint)lId;
                    barA.LRId[1] = (uint)rId;
                    barA.Bars.Clear();
                    for (int ibar = 0; ibar < nbar; ibar++)
                    {
                        int ibarTmp;
                        int iv0;
                        int iv1;
                        int s0;
                        int s1;
                        int r0;
                        int r1;
                        values = arch.GetValues();
                        ibarTmp = int.Parse(values[0]);
                        iv0 = int.Parse(values[1]);
                        iv1 = int.Parse(values[2]);
                        s0 = int.Parse(values[3]);
                        s1 = int.Parse(values[4]);
                        r0 = int.Parse(values[5]);
                        r1 = int.Parse(values[6]);
                        System.Diagnostics.Debug.Assert(ibarTmp == ibar);
                        System.Diagnostics.Debug.Assert(iv0 >= 0 && iv1 >= 0);
                        MeshBar bar = new MeshBar();
                        barA.Bars.Add(bar);
                        bar.V[0] = (uint)iv0;
                        bar.V[1] = (uint)iv1;
                        bar.S2[0] = (uint)s0;
                        bar.S2[1] = (uint)s1;
                        bar.R2[0] = (uint)r0;
                        bar.R2[1] = (uint)r1;
                    }
                }
                for (int iTriA = 0; iTriA < nTriA; iTriA++){
                    className = arch.ReadDepthClassName();
                    System.Diagnostics.Debug.Assert(className == "CTriAry2D");
                    int id;
                    values = arch.GetValues();
                    id = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(id > 0);
                    int cadId;
                    values = arch.GetValues();
                    cadId = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(cadId >= 0);
                    int ntri;
                    values = arch.GetValues();
                    ntri = int.Parse(values[0]);
                    System.Diagnostics.Debug.Assert(ntri > 0);
                    MeshTriArray2D triA = new MeshTriArray2D();
                    TriArrays.Add(triA);
                    triA.Id = (uint)id;
                    triA.LCadId = (uint)cadId;
                    for (int itri = 0; itri < ntri; itri++)
                    {
                        int itriTmp;
                        int iv0;
                        int iv1;
                        int iv2;
                        values = arch.GetValues();
                        itriTmp = int.Parse(values[0]);
                        iv0 = int.Parse(values[1]);
                        iv1 = int.Parse(values[2]);
                        iv2 = int.Parse(values[3]);
                        System.Diagnostics.Debug.Assert(itriTmp == itri);
                        System.Diagnostics.Debug.Assert(iv0 >= 0 && iv1 >= 0 && iv2 >= 0);
                        MeshTri2D tri = new MeshTri2D();
                        triA.Tris.Add(tri);
                        tri.V[0] = (uint)iv0;
                        tri.V[1] = (uint)iv1;
                        tri.V[2] = (uint)iv2;
                    }
                    {
                        uint npoin = (uint)Vecs.Count;
                        uint[] elsupInd = new uint[npoin + 1];
                        uint nelsup;
                        uint[] elsup;
                        MeshUtils2D.MakePointSurTri(triA.Tris, npoin, elsupInd, out nelsup, out elsup);
                        MeshUtils2D.MakeInnerRelationTri(triA.Tris, npoin, elsupInd, nelsup, elsup);
                    }
                }
                MakeElemLocationType();

                ////////////////////////////////
                // build the internal relationship
                for (int ibarary = 0; ibarary <BarArrays.Count; ibarary++)
                {
                    uint barMshId = BarArrays[ibarary].Id;
                    int barLoc = TypeLocs[(int)barMshId].Loc;
                    IList<MeshBar> bars = BarArrays[barLoc].Bars;
                    for (int isidebar = 0; isidebar < 2; isidebar++)
                    {
                        uint adjMshId = BarArrays[barLoc].LRId[isidebar];
                        if (adjMshId == 0)
                        {
                            continue;
                        }
                        //System.Diagnostics.Debug.WriteLine(adjMshId + " " + TypeLocs.Count);
                        System.Diagnostics.Debug.Assert(adjMshId < (int)TypeLocs.Count);
                        int adjLoc = TypeLocs[(int)adjMshId].Loc;
                        if (TypeLocs[(int)adjMshId].Type == 2)
                        {
                            // tri mesh
                            IList<MeshTri2D> tris = TriArrays[adjLoc].Tris;
                            for (int ibar = 0; ibar < bars.Count; ibar++)
                            {
                                uint itri = bars[ibar].S2[isidebar];
                                uint inotri = bars[ibar].R2[isidebar];
                                tris[(int)itri].G2[inotri] = (int)barMshId;
                                tris[(int)itri].S2[inotri] = (uint)ibar;
                                tris[(int)itri].R2[inotri] = (uint)isidebar;
                            }
                        }
                    }
                }
                System.Diagnostics.Debug.Assert(CheckMesh() == 0);
                arch.ShiftDepth(false);
                return true;
            }
            else
            {
                string line;

                // write file
                arch.WriteDepthClassName("CMesher2D");
                arch.ShiftDepth(true);

                ////////////////////////////////
                // write relation to CAD

                {
                    arch.WriteDepthClassName("setIdLCad_CutMesh");

                    line = string.Format("{0}", MeshingLoopCadIdELens.Count);
                    arch.WriteLine(line);

                    int icnt = 0;
                    foreach (var cadIdELen in MeshingLoopCadIdELens)
                    {
                        uint lId = cadIdELen.CadId;
                        line = string.Format("{0} {1}", icnt, lId);
                        arch.WriteLine(line);
                        icnt++;
                    }
                }

                {
                    // 旧型式のデータ
                    // -1をセットする
                    uint eSize0 = 0;
                    double eLen0 = -1.0;
                    line = string.Format("{0} {0} {0}", MeshingMode, eSize0, eLen0);
                    arch.WriteLine(line);

                    // 分解能をループ毎に指定できるようにしたバージョン
                    line = "";
                    for (int i = 0; i < MeshingLoopCadIdELens.Count; i++)
                    {
                        var cadIdELen = MeshingLoopCadIdELens[i];
                        double eLen = cadIdELen.ELen;
                        if (i != 0)
                        {
                            line += " ";
                        }
                        line += string.Format("{0}", eLen);
                    }
                }

                if (isOnlyCadMshLink)
                { // 
                    arch.ShiftDepth(false);
                    return true;
                }

                ////////////////////////////////
                // Write information of Msh

                line = string.Format("{0} {1}", Vecs.Count, 2);
                arch.WriteLine(line);
                for (int ivec = 0; ivec < Vecs.Count; ivec++)
                {
                    line = string.Format("{0} {1} {2}", ivec, Vecs[ivec].X, Vecs[ivec].Y);
                    arch.WriteLine(line);
                }
                int nQuadA = 0; // 未使用
                line = string.Format("{0} {1} {2} {3}",
                    Vertexs.Count, BarArrays.Count, TriArrays.Count, nQuadA);
                arch.WriteLine(line);
                {
                    // Vertexの出力
                    for (int iver = 0; iver < Vertexs.Count; iver++)
                    {
                        arch.WriteDepthClassName("SVertex");

                        line = string.Format("{0}", Vertexs[iver].Id);
                        arch.WriteLine(line);

                        line = string.Format("{0}", Vertexs[iver].VCadId);
                        arch.WriteLine(line);

                        line = string.Format("{0}", Vertexs[iver].V);
                        arch.WriteLine(line);
                    }
                }
                {
                    // Barの出力
                    for (int ibarary = 0; ibarary < BarArrays.Count; ibarary++)
                    {
                        arch.WriteDepthClassName("CBarAry");

                        line = string.Format("{0}", BarArrays[ibarary].Id);
                        arch.WriteLine(line);

                        line = string.Format("{0}", BarArrays[ibarary].ECadId);
                        arch.WriteLine(line);

                        line = string.Format("{0} {1}", BarArrays[ibarary].SEId[0], BarArrays[ibarary].SEId[1]);
                        arch.WriteLine(line);

                        line = string.Format("{0} {1}", BarArrays[ibarary].LRId[0], BarArrays[ibarary].LRId[1]);
                        arch.WriteLine(line);

                        line = string.Format("{0}", BarArrays[ibarary].Bars.Count);
                        arch.WriteLine(line);
                        IList<MeshBar> bars = BarArrays[ibarary].Bars;
                        for (int ibar = 0; ibar < bars.Count; ibar++)
                        {
                            line = string.Format("{0} {1} {2}  {3} {4}  {5} {6}", ibar,
                                bars[ibar].V[0], bars[ibar].V[1],
                                bars[ibar].S2[0], bars[ibar].S2[1],
                                bars[ibar].R2[0], bars[ibar].R2[1]);
                            arch.WriteLine(line);
                        }
                    }
                }
                {
                    // Triの出力
                    for (int itriary = 0; itriary < TriArrays.Count; itriary++)
                    {
                        arch.WriteDepthClassName("CTriAry2D");

                        line = string.Format("{0}", TriArrays[itriary].Id);
                        arch.WriteLine(line);

                        line = string.Format("{0}", TriArrays[itriary].LCadId);
                        arch.WriteLine(line);

                        line = string.Format("{0}", TriArrays[itriary].Tris.Count);
                        arch.WriteLine(line);
                        IList<MeshTri2D> tirs = TriArrays[itriary].Tris;
                        for (int itri = 0; itri < tirs.Count; itri++)
                        {
                            line = string.Format("{0} {1} {2} {3}",
                                itri, tirs[itri].V[0], tirs[itri].V[1], tirs[itri].V[2]);
                            arch.WriteLine(line);
                        }
                    }
                }
                arch.ShiftDepth(false);
            }
            return true;
        }

        private void MakeElemLocationType()
        {
            uint maxId = FindMaxId();
            TypeLocs = (new MeshTypeLoc[maxId + 1]).ToList();
            for (int iver = 0; iver < Vertexs.Count; iver++)
            {
                int id0 = (int)Vertexs[iver].Id;
                TypeLocs[id0].Loc = iver;
                TypeLocs[id0].Type = 0;
            }
            for (int ibarary = 0; ibarary < BarArrays.Count; ibarary++)
            {
                int id0 = (int)BarArrays[ibarary].Id;
                TypeLocs[id0].Loc = ibarary;
                TypeLocs[id0].Type = 1;
            }
            for (int itriary = 0; itriary < TriArrays.Count; itriary++)
            {
                int id0 = (int)TriArrays[itriary].Id;
                TypeLocs[id0].Loc = itriary;
                TypeLocs[id0].Type = 2;
            }
        }
    }
}
