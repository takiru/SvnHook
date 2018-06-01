using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSvn;
using System.Text.RegularExpressions;

namespace SvnHook.PreCommit
{
    /// <summary>
    /// pre-commitの制御を行います。
    /// </summary>
    class PreCommit
    {
        private SvnHookArguments hookArgs;

        /// <summary>
        /// pre-commitのロードを行います。
        /// </summary>
        /// <param name="args">呼出パラメーター。</param>
        /// <returns>true:成功, false:失敗。</returns>
        public bool Load(string[] args)
        {
            if (!SvnHookArguments.ParseHookArguments(args, SvnHookType.PreCommit, false, out hookArgs))
            {
                Console.Error.WriteLine("パラメーターが不正です。");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 検証を実施します。
        /// </summary>
        /// <returns>true:コミット可, false:コミット不可。</returns>
        public bool Validate()
        {
            using (SvnLookClient cl = new SvnLookClient())
            {
                SvnChangeInfoEventArgs ci;
                cl.GetChangeInfo(hookArgs.LookOrigin, out ci);

                // ログメッセージに関する検証
                if (!CommonHook.Validate(ci))
                {
                    return false;
                }

                // tagsに関する検証
                if (!TagsHook.Validate(ci))
                {
                    return false;
                }

                // branchesに関する検証
                if (!BranchesHook.Validate(ci))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
