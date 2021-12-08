using UnityEngine;

namespace Collisions.GPU
{
    public class Kernel
    {
        private int kernelID;
        private uint numThread_x;
        private uint numThread_y;
        private uint numThread_z;

        public int KernelID { get => kernelID; set => kernelID = value; }
        public uint NumThread_x { get => numThread_x; set => numThread_x = value; }
        public uint NumThread_y { get => numThread_y; set => numThread_y = value; }
        public uint NumThread_z { get => numThread_z; set => numThread_z = value; }

        public Kernel(ComputeShader cs, string name)
        {
            kernelID = cs.FindKernel(name);
            cs.GetKernelThreadGroupSizes(kernelID, out uint _numThread_x, out uint _numThread_y, out uint _numThread_z);

            numThread_x = _numThread_x;
            numThread_y = _numThread_y;
            numThread_z = _numThread_z;
        }
    }
}