using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace PluginBase.CommonUtils
{
    public static class DebugLogger
    {
        /// <summary>
        /// LEVEL 0 : 출력 X, 콘솔창 띄우지 X
        /// LEVEL 1 : 에러만 출력
        /// LEVEL 2 : 경고 출력
        /// LEVEL 3 : 확인용 로그까지 모두 출력
        /// </summary>
        private static int _debugLV;
        public static void LoggerInit(int level)
        {
            _debugLV = level;
            
            if (_debugLV > 0) AllocConsole();

            Log(3, $"[DEBUG] Debug Level set to \"{level}\""
                 + $"\n        0: No Debug Msg"
                 + $"\n        1: ERROR Only"
                 + $"\n        2: + WARNING"
                 + $"\n        3: All Debug Msgs");
        }
        public static void Log(int level, string msg)
        {
            if (level > _debugLV) return;
            
            if (level == 1) Console.ForegroundColor = ConsoleColor.Red;
            if (level == 2) Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(msg);

            Console.ForegroundColor = ConsoleColor.White;
        }

        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
    }
}