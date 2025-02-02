﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    public class FieldValue : IObject
    {
        public uint QuantityId { get; protected set; } = 0;
        public FieldValueType Type { get; protected set; } = FieldValueType.NoValue;
        public FieldDerivativeType DerivativeType { get; protected set; } = 0;
        public uint Dof { get; protected set; } = 1;
        public FieldValueNodeType NodeType { get; protected set; } = FieldValueNodeType.Node;
        public bool IsBubble
        {
            get
            {
                bool isBubble = false;
                if (NodeType == FieldValueNodeType.Node)
                {
                    isBubble = false;
                }
                else if (NodeType == FieldValueNodeType.Bubble)
                {
                    isBubble = true;
                }
                else
                {
                    // NodeかBubbleしか想定していない
                    System.Diagnostics.Debug.Assert(false);
                    throw new NotImplementedException();
                }
                return isBubble;
            }
        }
        public FieldShowType ShowType { get; protected set; } = FieldShowType.Real;
        public double[] DoubleValues { get; protected set; } = null;
        public double[] DoubleVelocityValues { get; protected set; } = null;
        public double[] DoubleAccelerationValues { get; protected set; } = null;
        public System.Numerics.Complex[] ComplexValues { get; protected set; } = null;
        public System.Numerics.Complex[] ComplexVelocityValues { get; protected set; } = null;
        public System.Numerics.Complex[] ComplexAccelerationValues { get; protected set; } = null;

        public FieldValue()
        {

        }

        public FieldValue(uint quantityId, FieldValueType type, FieldDerivativeType dt,
            FieldValueNodeType nodeType, FieldShowType showType, uint pointCnt)
        {
            QuantityId = quantityId;
            Type = type;
            Dof = GetDof(Type);
            DerivativeType = dt;
            NodeType = nodeType;
            ShowType = showType;
            AllocValues(pointCnt);
        }

        public FieldValue(FieldValue src)
        {
            Copy(src);
        }

        public void Copy(IObject src)
        {
            FieldValue srcFV = src as FieldValue;
            Type = srcFV.Type;
            DerivativeType = srcFV.DerivativeType;
            QuantityId = srcFV.QuantityId;
            Dof = srcFV.Dof;
            NodeType = srcFV.NodeType;
            ShowType = srcFV.ShowType;
            CopyValues(srcFV);
        }

        public static uint GetDof(FieldValueType valueType)
        {
            uint dof = 0;
            switch (valueType)
            {
                case FieldValueType.NoValue:
                    dof = 0;
                    break;
                case FieldValueType.Scalar:
                    dof = 1;
                    break;
                case FieldValueType.SymmetricTensor2:
                    dof = 3;
                    break;
                case FieldValueType.Vector2:
                    dof = 2;
                    break;
                case FieldValueType.Vector3:
                    dof = 3;
                    break;
                case FieldValueType.ZScalar:
                    dof = 1;
                    break;
                case FieldValueType.ZVector2:
                    dof = 2;
                    break;
                case FieldValueType.ZVector3:
                    dof = 3;
                    break;
                default:
                    dof = 0;
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
            return dof;
        }

        public void CopyValues(FieldValue src)
        {
            DoubleValues = null;
            if (src.DoubleValues != null)
            {
                DoubleValues = new double[src.DoubleValues.Length];
                src.DoubleValues.CopyTo(DoubleValues, 0);
            }
            DoubleVelocityValues = null;
            if (src.DoubleVelocityValues != null)
            {
                DoubleVelocityValues = new double[src.DoubleVelocityValues.Length];
                src.DoubleVelocityValues.CopyTo(DoubleVelocityValues, 0);
            }
            DoubleAccelerationValues = null;
            if (src.DoubleAccelerationValues != null)
            {
                DoubleAccelerationValues = new double[src.DoubleAccelerationValues.Length];
                src.DoubleAccelerationValues.CopyTo(DoubleAccelerationValues, 0);
            }

            ComplexValues = null;
            if (src.ComplexValues != null)
            {
                ComplexValues = new System.Numerics.Complex[src.ComplexValues.Length];
                src.ComplexValues.CopyTo(ComplexValues, 0);
            }
            ComplexVelocityValues = null;
            if (src.ComplexVelocityValues != null)
            {
                ComplexVelocityValues = new System.Numerics.Complex[src.ComplexVelocityValues.Length];
                src.ComplexVelocityValues.CopyTo(ComplexVelocityValues, 0);
            }
            ComplexAccelerationValues = null;
            if (src.ComplexAccelerationValues != null)
            {
                ComplexAccelerationValues = new System.Numerics.Complex[src.ComplexAccelerationValues.Length];
                src.ComplexAccelerationValues.CopyTo(ComplexAccelerationValues, 0);
            }
        }

        protected void AllocValues(uint pointCnt)
        {
            if (Type == FieldValueType.ZScalar ||
                Type == FieldValueType.ZVector2 ||
                Type == FieldValueType.ZVector3)
            {
                // complex
                if (DerivativeType.HasFlag(FieldDerivativeType.Value))
                {
                    ComplexValues = new System.Numerics.Complex[pointCnt * Dof];
                }
                if (DerivativeType.HasFlag(FieldDerivativeType.Velocity))
                {
                    ComplexVelocityValues = new System.Numerics.Complex[pointCnt * Dof];
                }
                if (DerivativeType.HasFlag(FieldDerivativeType.Acceleration))
                {
                    ComplexAccelerationValues = new System.Numerics.Complex[pointCnt * Dof];
                }
            }
            else
            {
                // double
                if (DerivativeType.HasFlag(FieldDerivativeType.Value))
                {
                    DoubleValues = new double[pointCnt * Dof];
                }
                if (DerivativeType.HasFlag(FieldDerivativeType.Velocity))
                {
                    DoubleVelocityValues = new double[pointCnt * Dof];
                }
                if (DerivativeType.HasFlag(FieldDerivativeType.Acceleration))
                {
                    DoubleAccelerationValues = new double[pointCnt * Dof];
                }
            }
        }

        public uint GetPointCount()
        {
            if (Type == FieldValueType.ZScalar)
            {
                if (ComplexValues == null)
                {
                    return 0;
                }
                return (uint)(ComplexValues.Length / Dof);
            }
            else
            {
                if (DoubleValues == null)
                {
                    return 0;
                }
                return (uint)(DoubleValues.Length / Dof);
            }
            throw new InvalidOperationException();
        }

        public double[] GetDoubleValues(FieldDerivativeType dt)
        {
            double[] values = null;
            if (dt.HasFlag(FieldDerivativeType.Value) && DoubleValues != null)
            {
                values = DoubleValues;
            }
            else if (dt.HasFlag(FieldDerivativeType.Velocity) && DoubleVelocityValues != null)
            {
                values = DoubleVelocityValues;
            }
            else if (dt.HasFlag(FieldDerivativeType.Acceleration) && DoubleAccelerationValues != null)
            {
                values = DoubleAccelerationValues;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return values;
        }

        public System.Numerics.Complex[] GetComplexValues(FieldDerivativeType dt)
        {
            System.Numerics.Complex[] values = null;
            if (dt.HasFlag(FieldDerivativeType.Value) && ComplexValues != null)
            {
                values = ComplexValues;
            }
            else if (dt.HasFlag(FieldDerivativeType.Velocity) && ComplexVelocityValues != null)
            {
                values = ComplexVelocityValues;
            }
            else if (dt.HasFlag(FieldDerivativeType.Acceleration) && ComplexAccelerationValues != null)
            {
                values = ComplexAccelerationValues;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return values;
        }

        public double[] GetDoubleValue(int coId, FieldDerivativeType dt)
        {
            double[] values = GetDoubleValues(dt);
            double[] value = new double[Dof];
            for (int iDof = 0; iDof < Dof; iDof++)
            {
                value[iDof] = values[coId * Dof + iDof];
            }
            return value;
        }

        public System.Numerics.Complex[] GetComplexValue(int coId, FieldDerivativeType dt)
        {
            System.Numerics.Complex[] values = GetComplexValues(dt);
            System.Numerics.Complex[] value = new System.Numerics.Complex[Dof];
            for (int iDof = 0; iDof < Dof; iDof++)
            {
                value[iDof] = values[coId * Dof + iDof];
            }
            return value;
        }

        public double GetShowValue(int coId, int iDof, FieldDerivativeType dt)
        {
            double value = 0;
            switch(ShowType)
            {
                case FieldShowType.Real:
                    {
                        double[] values = GetDoubleValues(dt);
                        value = values[coId * Dof + iDof];
                    }
                    break;
                case FieldShowType.Abs:
                    {
                        double[] values = GetDoubleValues(dt);
                        value = Math.Abs(values[coId * Dof + iDof]);
                    }
                    break;
                case FieldShowType.ZReal:
                    {
                        System.Numerics.Complex[] values = GetComplexValues(dt);
                        value = values[coId * Dof + iDof].Real;
                    }
                    break;
                case FieldShowType.ZImaginary:
                    {
                        System.Numerics.Complex[] values = GetComplexValues(dt);
                        value = values[coId * Dof + iDof].Imaginary;
                    }
                    break;
                case FieldShowType.ZAbs:
                    {
                        System.Numerics.Complex[] values = GetComplexValues(dt);
                        value = values[coId * Dof + iDof].Magnitude;
                    }
                    break;
            }
            return value;
        }

        public void GetMinMaxShowValue(out double min, out double max, int iDof, FieldDerivativeType dt)
        {
            min = Double.MaxValue;
            max = Double.MinValue;

            uint ptCnt = GetPointCount();
            for (int coId = 0; coId < ptCnt; coId++)
            {
                double value = GetShowValue(coId, iDof, dt);
                if (value < min)
                {
                    min = value;
                }
                if (value > max)
                {
                    max = value;
                }
            }
        }

    }
}
