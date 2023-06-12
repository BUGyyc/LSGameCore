namespace Lockstep.Util {
    public static partial class BitUtil {
        public static bool HasBit(int val, int idx){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"val: {val} idx: {idx} ");

            return (val & 1 << idx) != 0;
        }

        public static void SetBit(ref int val, int idx, bool isSet){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"val: {val} idx: {idx} ");

            if (isSet) {
                val |= (1 << idx);
            }
            else {
                val &= ~(1 << idx);
            }
        }

        public static bool HasBit(byte val, byte idx){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"");

            return (val & 1 << idx) != 0;
        }

        public static void SetBit(ref byte val, byte idx){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"");

            val |= (byte) (1 << idx);
        }

        public static byte ToByte(byte idx){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"");

            return (byte) (1 << idx);
        }
    }
}