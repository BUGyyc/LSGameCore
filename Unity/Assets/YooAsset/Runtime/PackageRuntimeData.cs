/*
 * @Author: delevin.ying
 * @Date: 2023-07-12 11:27:39
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-07-12 11:59:57
 */



public static class PackageRuntimeData
{
    public static string packageName;

    private static string _packageVersion;

    public static string packageVersion
    {
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                UnityEngine.Debug.LogError("version err");
            }
            _packageVersion = value;
        }
        get { return _packageVersion; }
    }

    public static string PackageVersion
    {
        get { return packageVersion; }
    }
}
