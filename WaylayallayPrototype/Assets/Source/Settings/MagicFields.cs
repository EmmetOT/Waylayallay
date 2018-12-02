using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Simplex
{
    /// <summary>
    /// Magic floats can behave like floats but they can also be 'magically' fixed
    /// to a settings value which can change at runtime.
    /// 
    /// Note that assigning a magic float with any kind of operation will break its link.
    /// </summary>
    [System.Serializable]
    public class MagicFloat
    {
        [SerializeField]
        private float m_fixedValue;
        public float FixedValue { get { return m_fixedValue; } }

        [SerializeField]
        private UniversalControlSettings.Setting m_setting;
        public UniversalControlSettings.Setting Setting { get { return m_setting; } }

        // indicates that the value needs to be recalculated
        private bool m_dirty = true;
        private float m_value = 0f;

        public MagicFloat(float fixedValue) : this(UniversalControlSettings.Setting.FIXED, fixedValue) { }

        public MagicFloat(UniversalControlSettings.Setting setting) : this(setting, 0f) { }

        public MagicFloat(UniversalControlSettings.Setting setting, float fixedValue, List<MagicOp> ops = null)
        {
            m_fixedValue = fixedValue;
            m_setting = setting;

            if (ops != null)
                m_ops = ops;

            UniversalControlSettings.SetDirtyEvent += SetDirty;
        }

        public static MagicFloat Copy(MagicFloat input)
        {
            MagicFloat output = new MagicFloat(input.Setting, input.FixedValue, input.m_ops);
            output.m_ops = new List<MagicOp>(input.m_ops);

            return output;
        }

        public void SetDirty()
        {
            m_dirty = true;
        }

        private float GetBaseValue()
        {
            if (m_setting != UniversalControlSettings.Setting.FIXED)
                return Manager.UniversalControlSettings.GetSetting(m_setting);

            return m_fixedValue;
        }

        public float GetValue()
        {
            if (!m_dirty)
                return m_value;
            
            m_value = GetBaseValue();

            for (int i = 0; i < m_ops.Count; i++)
            {
                if (m_ops[i].Op == MagicOp.Operation.ADD)
                    m_value += m_ops[i].MagicFloat.GetValue();
                else if (m_ops[i].Op == MagicOp.Operation.MULTIPLY)
                    m_value *= m_ops[i].MagicFloat.GetValue();
                else if (m_ops[i].Op == MagicOp.Operation.DIVIDE)
                    m_value /= m_ops[i].MagicFloat.GetValue();
            }

            m_dirty = false;

            return m_value;
        }

        public void AddOp(MagicOp op)
        {
            if (ReferenceEquals(this, op))
                Debug.LogWarning("Trying to operate on a MagicFloat using itself, will probably cause an infinite loop.");

            m_ops.Add(op);
        }

        public static implicit operator float(MagicFloat magic)
        {
            // this is where the magic happens ;)
            return magic.GetValue();
        }

        public static implicit operator MagicFloat(float num)
        {
            return new MagicFloat(num);
        }

        public static implicit operator MagicFloat(UniversalControlSettings.Setting setting)
        {
            return new MagicFloat(setting);
        }

        public static bool operator ==(MagicFloat one, MagicFloat two)
        {
            return one.GetValue() == two.GetValue();
        }

        public static bool operator !=(MagicFloat one, MagicFloat other)
        {
            return !(one == other);
        }

        public static MagicFloat operator +(MagicFloat one, MagicFloat two)
        {
            MagicFloat copy = Copy(one);
            copy.AddOp(new MagicOp(two, MagicOp.Operation.ADD));

            return copy;
        }

        public static MagicFloat operator +(MagicFloat one, UniversalControlSettings.Setting two)
        {
            MagicFloat copy = Copy(one);
            copy.AddOp(new MagicOp(two, MagicOp.Operation.ADD));

            return copy;
        }

        public static MagicFloat operator -(MagicFloat one)
        {
            MagicFloat copy = Copy(one);
            copy.AddOp(new MagicOp(-1f, MagicOp.Operation.MULTIPLY));

            return copy;
        }

        public static MagicFloat operator -(MagicFloat one, MagicFloat two)
        {
            MagicFloat copy = Copy(one);
            copy.AddOp(new MagicOp(-two, MagicOp.Operation.ADD));

            return copy;
        }

        public static MagicFloat operator /(MagicFloat one, MagicFloat two)
        {
            MagicFloat copy = Copy(one);
            copy.AddOp(new MagicOp(two, MagicOp.Operation.DIVIDE));

            return copy;
        }

        public static MagicFloat operator *(MagicFloat one, MagicFloat two)
        {
            MagicFloat copy = Copy(one);
            copy.AddOp(new MagicOp(two, MagicOp.Operation.MULTIPLY));
            
            return copy;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(MagicFloat))
                return false;

            MagicFloat other = (MagicFloat)obj;

            return this == other;
        }

        public override int GetHashCode()
        {
            if (m_setting == UniversalControlSettings.Setting.FIXED)
                return m_fixedValue.GetHashCode();

            return (int)m_setting;
        }

        public override string ToString()
        {
            return ((float)this).ToString();
        }

        private List<MagicOp> m_ops = new List<MagicOp>();

        public struct MagicOp
        {
            public MagicFloat MagicFloat { get; }
            public Operation Op { get; }

            public MagicOp(MagicFloat fl, Operation op)
            {
                MagicFloat = fl;
                Op = op;
            }

            public enum Operation { ADD, MULTIPLY, DIVIDE };
        }
    }

    /// <summary>
    /// Magic floats can behave like Vector3s but each of their values is a MagicFloat.
    /// 
    /// (Also, they're serializable.)
    /// </summary>
    [System.Serializable]
    public struct MagicVector3
    {
        [SerializeField]
        private MagicFloat m_x;
        public MagicFloat x { get { return m_x; } }

        [SerializeField]
        private MagicFloat m_y;
        public MagicFloat y { get { return m_y; } }

        [SerializeField]
        private MagicFloat m_z;
        public MagicFloat z { get { return m_z; } }

        public MagicVector3(Vector3 vec) : this(vec.x, vec.y, vec.z) { }

        public MagicVector3(MagicFloat x, MagicFloat y, MagicFloat z)
        {
            m_x = x;
            m_y = y;
            m_z = z;
        }

        public Vector3 GetValue()
        {
            return new Vector3(m_x, m_y, m_z);
        }

        public static implicit operator Vector3(MagicVector3 mV)
        {
            return mV.GetValue();
        }

        public static implicit operator MagicVector3(Vector3 vec)
        {
            return new MagicVector3(vec);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(MagicVector3))
                return false;

            MagicVector3 other = (MagicVector3)obj;

            return this == other;
        }

        public static bool operator ==(MagicVector3 one, MagicVector3 two)
        {
            return one.x == two.x && one.y == two.y && one.z == two.z;
        }

        public static bool operator !=(MagicVector3 one, MagicVector3 two)
        {
            return !(one == two);
        }

        public static MagicVector3 operator *(MagicVector3 one, MagicFloat two)
        {
            return new MagicVector3(one.x * two, one.y * two, one.z * two);
        }

        public static MagicVector3 operator *(MagicFloat two, MagicVector3 one)
        {
            return new MagicVector3(one.x * two, one.y * two, one.z * two);
        }

        public static MagicVector3 operator /(MagicVector3 one, MagicFloat two)
        {
            return new MagicVector3(one.x / two, one.y / two, one.z / two);
        }

        public static MagicVector3 operator /(MagicFloat two, MagicVector3 one)
        {
            return new MagicVector3(one.x / two, one.y / two, one.z / two);
        }

        public override int GetHashCode()
        {
            return m_x.GetHashCode() + (17 * m_y.GetHashCode() + 17 * (m_z.GetHashCode()));
        }

        public MagicVector3 SetX(MagicFloat x)
        {
            return new MagicVector3(x, m_y, m_z);
        }

        public MagicVector3 SetY(MagicFloat y)
        {
            return new MagicVector3(m_x, y, m_z);
        }

        public MagicVector3 SetZ(MagicFloat z)
        {
            return new MagicVector3(m_x, m_y, z);
        }

        public override string ToString()
        {
            return "(" + m_x.ToString() + ", " + m_y.ToString() + ", " + m_z.ToString() + ")";
        }
    }
}
