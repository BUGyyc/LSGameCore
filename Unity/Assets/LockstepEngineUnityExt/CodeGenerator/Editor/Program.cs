using System;
using System.Collections.Generic;
using System.IO;
using Lockstep.Util;
using UnityEngine;
using Debug = Lockstep.Logging.Debug;
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

            List<string> list = new List<string>();

            FileUtil.GetDir(@"F:\github\Lockstep-Tutorial\Unity\Assets\Scripts", ".cs", ref list);
            foreach (string path in list)
            {
                LogMaster.I("--->"+path);
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

        static string pattern = @"(public|private|protected)((\s+(static|override|virtual)*\s+)|\s+)\w+(<\w+>)*(\[\])*\s+\w+(<\w+>)*\s*\(([^\)]+\s*)?\)\s*\{[^\{\}]*(((?'Open'\{)[^\{\}]*)+((?'-Open'\})[^\{\}]*)+)*(?(Open)(?!))\}";


        //static string pattern = @"(public|private|protected)((\s+(static|override|virtual)*\s+)|\s+)\w+(＜\w+>)*(\[\])*\s+\w+(＜\w+>)*\s*\(([^\)]+\s*)?\)";

        /**
         * 
         *     (public|private|protected)((\s+(static|override|virtual)*\s+)|\s+)\w+(＜\w+>)*(\[\])*\s+\w+(＜\w+>)*\s*\(([^\)]+\s*)? \)\s*\{[^\{\}]*(((? 'Open'\{)[^\{\}]*)+((? '-Open'\})[^\{\}]*)+)*(? (Open)(? ! ))\}
         * 
         * 
         * **/


        static Regex ms_regexFuncAll;// = new Regex(pattern);

        public static void InsertLogTrackCode(string path)
        {
            string baseDir = "";
            string subPath = "";

            bool hasChanged = false;
            var fullPath = path;
            //baseDir + subPath;
            if (!File.Exists(fullPath))
            {
                LogMaster.E("不存在文件", fullPath);
                return;
            }

            ms_regexFuncAll = new Regex(pattern);

            var text = File.ReadAllText(fullPath);
            LogMaster.A(text);
            var matches = ms_regexFuncAll.Matches(text);
            int cnt = matches.Count;

            LogMaster.I("text.Len", text.Length.ToString());

            LogMaster.E("匹配数量", cnt.ToString());

            //for (int i = cnt - 1; i >= 0; i--)
            //{
            //    var matchFuncAll = matches[i];
            //    var matchFuncHead = ms_regexFuncHead.Match(
            //        text,
            //        matchFuncAll.Index,
            //        matchFuncAll.Length
            //    );

            //    var matchLeftBrace = ms_regexLeftBrace.Match(
            //        text,
            //        matchFuncAll.Index,
            //        matchFuncAll.Length
            //    );

            //    if (matchLeftBrace.Success) // 如果没找到，则不是一个规则的函数体，不打印日志
            //    {
            //        // 如果找到第1个左括号，则寻找第1行代码
            //        int len =
            //            matchFuncAll.Index
            //            + matchFuncAll.Length
            //            - (matchLeftBrace.Index + matchLeftBrace.Length);

            //        var matchFirstCode = ms_regexFirstCode.Match(
            //            text,
            //            matchLeftBrace.Index + matchLeftBrace.Length,
            //            len
            //        );

            //        if (matchFirstCode.Success) // 如果没有找到，则是一个空函数，不需要打印日志
            //        {
            //            // 如果找到代码，则判断是否是日志代码
            //            if (!ms_regexLogTrackCode.IsMatch(matchFirstCode.Value))
            //            {
            //                if (!ms_regexLogTrackCodeIgnore.IsMatch(matchFirstCode.Value))
            //                {
            //                    // 如果不是日志代码，则需要打印日志
            //                    string textLogCode = GetLogTrackCode(matchFuncHead.Value);
            //                    // 不增加文件的行数
            //                    text = text.Insert(
            //                        matchLeftBrace.Index + matchLeftBrace.Length,
            //                        textLogCode
            //                    );

            //                    hasChanged = true;
            //                }
            //            }
            //        }
            //    }
            //}

            if (hasChanged)
            {
                File.WriteAllText(baseDir + subPath, text);
            }
        }
    }
#endif
}
