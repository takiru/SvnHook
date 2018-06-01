using SharpSvn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvnHook.PreCommit
{
    /// <summary>
    /// 共通的な操作するためのフックスクリプト。
    /// </summary>
    public class CommonHook
    {
        /// <summary>
        /// 共通検証。
        /// </summary>
        /// <param name="ci">SvnChangeInfoEventArgs オブジェクト。</param>
        /// <returns>True:有効, False:無効。</returns>
        public static bool Validate(SvnChangeInfoEventArgs ci)
        {
            if (IsNotInputMessage(ci))
            {
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine("ログメッセージが未入力です。");
                Console.WriteLine("----------------------------------------------------------");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ログメッセージが未入力かどうかを取得する。
        /// </summary>
        /// <param name="ci">SvnChangeInfoEventArgs オブジェクト。</param>
        /// <returns>true:未入力, false:入力済み。</returns>
        private static bool IsNotInputMessage(SvnChangeInfoEventArgs ci)
        {
            if (string.IsNullOrEmpty(ci.LogMessage.Trim()))
            {
                return true;
            }
            return false;
        }
    }
}
