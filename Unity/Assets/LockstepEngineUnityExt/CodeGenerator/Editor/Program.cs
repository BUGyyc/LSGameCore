using System;
using System.Collections.Generic;
using System.IO;
using Lockstep.Util;
using UnityEngine;
using Debug = Lockstep.Logging.Debug;
using System.Text;
using static UnityEditor.Progress;
#if UNITY_5_3_OR_NEWER
using UnityEditor;
using Lockstep.Game;

#endif

using System.Text.RegularExpressions;

namespace Lockstep.CodeGenerator
{
    public class CodeGenHelper
    {
        public class CodeGenInfos
        {
            public GenInfo[] GenInfos;
        }

        public static void Gen(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                args = new[] { "../Config/CodeGenerator/Config.json" };
            }

            if (args.Length > 0)
            {
                foreach (var path in args)
                {
                    Debug.Log(path);
                    CopyFilesByConfig(Path.Combine(Define.BaseDirectory, path));
                }
            }
            else
            {
                Debug.Log("Need config path");
            }
        }

        static void CopyFilesByConfig(string configPath)
        {
            LogMaster.S($"read config", configPath);

            var allTxt = File.ReadAllText(configPath);
            var config = JsonUtil.ToObject<CodeGenInfos>(allTxt);
            var prefix = Define.BaseDirectory;
            foreach (var genInfo in config.GenInfos)
            {
                GenCode(genInfo);
            }
        }

        static void GenCode(GenInfo info)
        {
            EditorBaseCodeGenerator gener = null;
            if (info == null || string.IsNullOrEmpty(info.GenerateFileName))
                return;
            var path = Path.Combine(Define.BaseDirectory, info.TypeHandlerConfigPath);
            // Debug.Log(path);
            LogMaster.S($"GenCode by file", path);
            var allTxt = File.ReadAllText(path);
            var config = JsonUtil.ToObject<FileHandlerInfo>(allTxt);
            info.FileHandlerInfo = config;
            gener = new EditorBaseCodeGenerator(info) { };
            gener.HideGenerateCodes();
            gener.BuildProject();
            gener.GenerateCodeNodeData(true);
        }
    }

#if !UNITY_5_3_OR_NEWER
    internal class Program
    {
        public static void Main(string[] args)
        {
            CodeGenHelper.Gen(args);
        }
    }
#else

    public static class EditorCodeGen
    {
        [MenuItem("LPEngine/CodeGen")]
        static void CodeGen()
        {
            LogMaster.I("[Tool] 代码自动生成----------- ");
            Lockstep.Logging.Logger.OnMessage += UnityLogHandler.OnLog;
            var config = Resources.Load<CodeGenConfig>("CodeGenerator/CodeGenConfig");
            Define.RelPath = config.relPath;
            var path = Define.BaseDirectory;
            CodeGenHelper.Gen(config.args.Split(';'));
        }

        [MenuItem("LPEngine/Create Lockstep Log")]
        static void GenerateLockstepLog()
        {
            //Directory dir = new Directory();


            //var arr =new string[] { "Vector3 pointOfPlane", "int abc" };
            //Regex reg = new Regex("(int )");
            //foreach(var item in arr)
            //{
            //  if(reg.Match(item).Success)
            //    {
            //        LogMaster.I(item);
            //    }
            //}

            //return;
            List<string> list = new List<string>();

            //InsertLogTrackCode(@"F:\github\Lockstep-Tutorial\Unity\Assets\Scripts\LSCommon\Launch.cs");

            FileUtil.GetDir(@"F:\github\Lockstep-Tutorial\Unity\Assets\Scripts", ".cs", ref list);
            foreach (string path in list)
            {
                LogMaster.I("--->" + path);
                InsertLogTrackCode(path);
            }
        }

        //public static void HashLogTrackCode(string baseDir, string subPath, LogTrackPdbFile pdb, LogHashType hashType)
        //{
        //    bool hasChanged = false;
        //    var fullPath = baseDir + subPath;
        //    if (!File.Exists(fullPath))
        //    {
        //        return;
        //    }

        //    var lines = File.ReadAllLines(fullPath);
        //    for (int i = 0; i < lines.Length; i++)
        //         {
        //        var line = lines[i];
        //        var matchLogCode = ms_regexLogTrackCode.Match(line);
        //        if (matchLogCode.Success)
        //        {
        //            var matchLogHash = ms_regexNumber.Match(
        //                line, matchLogCode.Index, matchLogCode.Length);

        //            if (matchLogHash.Success)
        //            {
        //                int hash = 0;
        //                int.TryParse(matchLogHash.Value, out hash);
        //                int argCnt = GetLogTrackArgCnt(matchLogCode.Value);

        //                // 寻找可能的注释
        //                var dbgStr = GetLogTrackDebugString(
        //                    ref line, matchLogCode.Index + matchLogCode.Length);
        // @
        //                   if ((hashType == LogHashType.NewHash && hash == 0) ||
        //                       (hashType == LogHashType.OldHash && hash! = 0))
        //                {
        //                    int validHash = pdb.AddItem(
        //                      hash, argCnt, subPath, i + 1, dbgStr);
        //                    if (validHash! = hash)
        //                    {
        //                        // 替换Hash值
        //                        line = line.Remove(
        //                          matchLogHash.Index, matchLogHash.Length);

        //                        line = line.Insert(
        //                          matchLogHash.Index, validHash.ToString());

        //                        lines[i] = line;
        //                        hasChanged = true;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (hasChanged)
        //    {
        //        File.WriteAllLines(baseDir + subPath, lines);
        //    }
        //}

        //public static void HandleLogTrackMacro(string baseDir, string subPath)
        //{
        //    if (ms_logTrackMacro == ms_logTrackClass)
        //    {
        //        return;
        //    }

        //    bool hasChanged = false;

        //    var fullPath = baseDir + subPath;
        //    if (!File.Exists(fullPath))
        //    {
        //        return;
        //    }

        //    var lines = File.ReadAllLines(fullPath);
        //    for (int i = 0; i < lines.Length; i++){
        //        var line = lines[i];
        //        if (ms_regexLogTrackMacro.IsMatch(line))
        //        {
        //            line = line.Replace(ms_logTrackMacro, ms_logTrackClass);
        //            lines[i] = line;
        //            hasChanged = true;
        //        }
        //    }
        //    if (hasChanged)
        //    {
        //        File.WriteAllLines(baseDir + subPath, lines);
        //    }
        //}

        static string func_pattern =
            @"(public|private|protected)((\s+(static|override|virtual)*\s+)|\s+)\w+(<\w+>)*(\[\])*\s+\w+(<\w+>)*\s*\(([^\)]+\s*)?\)\s*\{[^\{\}]*(((?'Open'\{)[^\{\}]*)+((?'-Open'\})[^\{\}]*)+)*(?(Open)(?!))\}";

        static string head_pattern =
            @"(public|private|protected)((\s+(static|override|virtual)*\s+)|\s+)\w+(<\w+>)*(\[\])*\s+\w+(<\w+>)*\s*\(([^\)]+\s*)?\)";

        static string left_pattern = @"{.";

        static string first_pattern = @"[a-z]+";

        static string log_pattern = @"LogMaster\.L\(.";

        static string ignore_log_pattern = @"Debug\..";

        static Regex ms_regexFuncAll; // = new Regex(pattern);
        static Regex ms_regexFuncHead;
        static Regex ms_regexLeftBrace;
        static Regex ms_regexFirstCode;
        static Regex ms_regexLogTrackCode;
        static Regex ms_regexLogTrackCodeIgnore;

        public static void InsertLogTrackCode(string path)
        {
            //string baseDir = "";
            //string subPath = "";

            bool hasChanged = false;
            var fullPath = path;
            //baseDir + subPath;
            if (!File.Exists(fullPath))
            {
                LogMaster.E("不存在文件", fullPath);
                return;
            }

            ms_regexFuncAll = new Regex(func_pattern);

            ms_regexFuncHead = new Regex(head_pattern);

            ms_regexLeftBrace = new Regex(left_pattern);

            ms_regexFirstCode = new Regex(first_pattern);

            ms_regexLogTrackCode = new Regex(log_pattern);

            ms_regexLogTrackCodeIgnore = new Regex(ignore_log_pattern);

            var text = File.ReadAllText(fullPath);
            // LogMaster.A(text);
            var matches = ms_regexFuncAll.Matches(text);
            int cnt = matches.Count;

            // LogMaster.I("text.Len", text.Length.ToString());

            // LogMaster.E("匹配数量", cnt.ToString());

            for (int i = cnt - 1; i >= 0; i--)
            {
                var matchFuncAll = matches[i];

                if (matchFuncAll.Value.Contains(LockstepLogFlag))
                {
                    //
                    LogMaster.I("has old flag");
                    continue;
                }

                // LogMaster.A("func content: " + matchFuncAll.Value);
                var matchFuncHead = ms_regexFuncHead.Match(
                    text,
                    matchFuncAll.Index,
                    matchFuncAll.Length
                );

                var matchLeftBrace = ms_regexLeftBrace.Match(
                    text,
                    matchFuncAll.Index,
                    matchFuncAll.Length
                );

                if (matchLeftBrace.Success) // 如果没找到，则不是一个规则的函数体，不打印日志
                {
                    // 如果找到第1个左括号，则寻找第1行代码
                    int len =
                        matchFuncAll.Index
                        + matchFuncAll.Length
                        - (matchLeftBrace.Index + matchLeftBrace.Length);

                    var matchFirstCode = ms_regexFirstCode.Match(
                        text,
                        matchLeftBrace.Index + matchLeftBrace.Length,
                        len
                    );

                    if (matchFirstCode.Success) // 如果没有找到，则是一个空函数，不需要打印日志
                    {
                        //LogMaster.I("find success  ---------------------" + matchFirstCode.Value);
                        // 如果找到代码，则判断是否是日志代码
                        if (!ms_regexLogTrackCode.IsMatch(matchFirstCode.Value))
                        {
                            //LogMaster.I("find success  ---------------------" + matchFirstCode.Value);
                            if (!ms_regexLogTrackCodeIgnore.IsMatch(matchFirstCode.Value))
                            {
                                //LogMaster.I("find success  333333333333333333333---------------------" + matchFirstCode.Value);



                                // 如果不是日志代码，则需要打印日志
                                string textLogCode = GetLogTrackCode(
                                    matchFuncHead.Value,
                                    out bool flag
                                );

                                //// 不增加文件的行数
                                text = text.Insert(
                                    matchLeftBrace.Index + matchLeftBrace.Length,
                                    textLogCode
                                );

                                hasChanged = true && flag;

                                if (hasChanged)
                                    LogMaster.I("添加打印 " + matchFuncHead.Value);
                            }
                        }
                    }
                }
            }

            if (hasChanged)
            {
                File.WriteAllText(fullPath, text);
            }
        }

        static List<string> ParamCompareList = new List<string>() {"uint","int", "long" , "LFloat", "LVector3" };

        const string LockstepLogFlag = "//NOTE: AutoCreate LockstepLog";

        private static string GetLogTrackCode(string str, out bool flag)
        {
            //string logMasterStr = string.Format($"hello world");
            //string logStr = string.Format($"\n\r LogMaster.L(\"{logMasterStr}\");");

            List<string> result = new List<string>();

            var a = str.Replace("ref ", "");
            var b = a.Replace("out ", "");

            string reStr = b.Replace(')', ' ');

            string[] array = reStr.Split('(');
            reStr = array[1];

            Regex compareReg = default;

            string[] paramGroup = reStr.Split(',');
            foreach (var m in paramGroup)
            {
                //LogMaster.L("m " + m);
                foreach (var item in ParamCompareList)
                {

                    compareReg = new Regex("("+ item+" )");

                    if (compareReg.Match(m).Success)
                    {
                        var newM = m.Replace(item+" ", "");
                        // LogMaster.L("result:  " + newM);
                        result.Add(newM);
                        break;
                    }
                }

            }

            string logMasterStr = default;
            StringBuilder stringBuilder = new StringBuilder();

            foreach (var item in result)
            {
                LogMaster.L("item " + item);
                //logMasterStr += @"item:{item}";
                string val = string.Copy(item).Trim();
                stringBuilder.Append(val + ": ");
                stringBuilder.Append(@"{");
                stringBuilder.Append(val);
                stringBuilder.Append(@"} ");
            }

            //stringBuilder.Append(");");
            logMasterStr = stringBuilder.ToString();
            //string logStr = stringBuilder.ToString();

            string logStr =
                "        "
                + LockstepLogFlag
                + $"\n        LogMaster.L($\""
                + logMasterStr
                + "\");\n";

            flag = result.Count > 0;

            if (flag == false)
            {
                //LogMaster.E("字段不符合 ");
            }
            else
            {
                //LogMaster.L("字段符合 ");
            }

            return logStr;
        }
    }
#endif
}
