using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 本机空间 → 会话空间的刚体变换。
    ///
    /// 单机/同原点时是单位变换；双设备时由三点标定解出（见下）。
    /// 会话空间定义：以 Host 采集三个物理标记点时的本机空间为基准，
    /// 其余设备各自解出「本机 → Host 空间」的旋转+平移。
    /// </summary>
    public struct BpfSessionTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool valid;

        public static BpfSessionTransform Identity =>
            new BpfSessionTransform
            {
                position = Vector3.zero,
                rotation = Quaternion.identity,
                valid = true,
            };

        public Vector3 ApplyToPosition(Vector3 local)
        {
            return rotation * local + position;
        }

        public Quaternion ApplyToRotation(Quaternion local)
        {
            return rotation * local;
        }

        /// <summary>会话空间坐标 → 本机空间坐标（接收远端位姿时用）。</summary>
        public Vector3 ApplyInverseToPosition(Vector3 session)
        {
            return Quaternion.Inverse(rotation) * (session - position);
        }

        /// <summary>会话空间旋转 → 本机空间旋转（接收远端位姿时用）。</summary>
        public Quaternion ApplyInverseToRotation(Quaternion session)
        {
            return Quaternion.Inverse(rotation) * session;
        }
    }

    /// <summary>
    /// 三点刚体解算：已知三个点在本机空间的坐标 q1..q3，以及同一组物理点
    /// 在会话空间的坐标 p1..p3，解出把本机空间对齐到会话空间的刚体变换。
    ///
    /// 方法：分别以两个三点组构造正交基，R = 会话基 × 本机基ᵀ，
    /// 平移 t = p1 − R·q1。三点共线或重合时解退化，返回 false。
    /// </summary>
    public static class BpfSessionSpace
    {
        private const float MinEdgeLength = 0.01f;  // 1cm 以下视为退化
        private const float MinTriangleArea = 0.0005f;

        public static bool TrySolve(
            Vector3 q1, Vector3 q2, Vector3 q3,
            Vector3 p1, Vector3 p2, Vector3 p3,
            out BpfSessionTransform result)
        {
            result = default;

            if (!TryBuildBasis(q1, q2, q3, out Matrix4x4 localBasis))
            {
                return false;
            }

            if (!TryBuildBasis(p1, p2, p3, out Matrix4x4 sessionBasis))
            {
                return false;
            }

            // 正交基：逆 = 转置
            Matrix4x4 rotationMatrix = sessionBasis * localBasis.transpose;

            Vector3 forward = rotationMatrix.MultiplyVector(Vector3.forward).normalized;
            Vector3 up = rotationMatrix.MultiplyVector(Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, up);

            Vector3 translation = p1 - (rotation * q1);

            result = new BpfSessionTransform
            {
                position = translation,
                rotation = rotation,
                valid = true,
            };
            return true;
        }

        /// <summary>
        /// 由三个点构造右手正交基矩阵（列为 x/y/z 轴）。退化时返回 false。
        /// </summary>
        private static bool TryBuildBasis(Vector3 a, Vector3 b, Vector3 c, out Matrix4x4 basis)
        {
            basis = Matrix4x4.identity;

            Vector3 edgeAB = b - a;
            Vector3 edgeAC = c - a;

            if (edgeAB.magnitude < MinEdgeLength || edgeAC.magnitude < MinEdgeLength)
            {
                return false;
            }

            Vector3 normal = Vector3.Cross(edgeAB, edgeAC);
            if (normal.magnitude * 0.5f < MinTriangleArea)
            {
                return false; // 三点几乎共线
            }

            Vector3 xAxis = edgeAB.normalized;
            Vector3 zAxis = normal.normalized;
            Vector3 yAxis = Vector3.Cross(zAxis, xAxis).normalized;

            basis.SetColumn(0, new Vector4(xAxis.x, xAxis.y, xAxis.z, 0f));
            basis.SetColumn(1, new Vector4(yAxis.x, yAxis.y, yAxis.z, 0f));
            basis.SetColumn(2, new Vector4(zAxis.x, zAxis.y, zAxis.z, 0f));
            basis.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
            return true;
        }

        /// <summary>调试用：把变换转成一行文本。</summary>
        public static string Describe(in BpfSessionTransform transform)
        {
            if (!transform.valid)
            {
                return "invalid";
            }

            return "pos=" + transform.position.ToString("F3")
                + " rot=" + transform.rotation.eulerAngles.ToString("F1");
        }
    }
}
