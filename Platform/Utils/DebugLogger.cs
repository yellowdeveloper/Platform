using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace Platform.Utils
{
    public static class DebugLogger
    {
        /// <summary>
        /// LEVEL 0 : 에러만 출력
        /// LEVEL 1 : 경고 출력
        /// LEVEL 2 : 확인용 로그까지 모두 출력
        /// </summary>
        private static int _debugLV;
        public static void LoggerInit(int level)
        {
            _debugLV = level;
            
            if (_debugLV > 0) AllocConsole();

            Log(1, $"[DEBUG] Debug Level set to \"{level}\""
                 + $"\n        0: No Debug Msg"
                 + $"\n        1: ERROR Only"
                 + $"\n        2: + WARNING"
                 + $"\n        3: All Debug Msgs");
        }
        public static void Log(int level, string msg)
        {
            if (level > _debugLV) return;
            
            Console.WriteLine(msg);
        }

        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
    }
}
