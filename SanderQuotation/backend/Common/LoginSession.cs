using Business.Common;

namespace backend.Common
{
    public class LoginSession
    {
        static SessionVO _session;
        static Dictionary<string, List<String>> blockIdMap;
        // Gets the current session.
        public static SessionVO Current
        {
            get
            {
                var str = HttpContext.Current.Session.GetString("__MySession__");
                //SessionVO session;
                if (string.IsNullOrEmpty(str))
                {
                    _session = new();
                    HttpContext.Current.Session.SetString("__MySession__", Newtonsoft.Json.JsonConvert.SerializeObject(_session));
                }
                else
                    _session = Newtonsoft.Json.JsonConvert.DeserializeObject<SessionVO>(HttpContext.Current.Session.GetString("__MySession__"));

                return _session;
            }

            set
            {
                _session = value;
                HttpContext.Current.Session.SetString("__MySession__", Newtonsoft.Json.JsonConvert.SerializeObject(_session));
            }
        }



        /// <summary>
        /// 驗證目前登入者是否能觀看指定頁面
        /// </summary>
        /// <param name="FuncCode"></param>
        /// <returns></returns>
        public static bool Authorization(IEnumerable<int> functions)
        {
            var list = Current.Functions;
            if (list == null || list.Count() == 0) return false;

            if (functions.Where(w => w == 0).Count() > 0)
            {
                //如果是首頁，登入就可以進入
                return true;
            }
            var result = functions.Any(x => list.Select(y => y.FunctionId).Contains(x));
            return result;
        }


        //處理分批上傳檔案 放在session 的key
        private static string SESSION_CHUNK_KEY = "__sessionChunkKey__";

        /// <summary>
        /// 記錄blockId至session
        /// </summary>
        /// <param name="uploadUid"></param>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public static List<String> SaveChunkData(string uploadUid, string blockId)
        {
            blockIdMap = GetChunkData(uploadUid);

            //加入至session
            blockIdMap[uploadUid].Add(blockId);
            HttpContext.Current.Session.SetString(SESSION_CHUNK_KEY, Newtonsoft.Json.JsonConvert.SerializeObject(blockIdMap));

            return blockIdMap[uploadUid];
        }

        private static Dictionary<string, List<String>> GetChunkData(string uploadUid)
        {

            var str = HttpContext.Current.Session.GetString(SESSION_CHUNK_KEY);
            if (str == null)
            {
                blockIdMap = new Dictionary<string, List<String>>();
            }

            //記錄blockId
            blockIdMap = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<String>>>(HttpContext.Current.Session.GetString(SESSION_CHUNK_KEY));
            if (!blockIdMap.ContainsKey(uploadUid))
            {
                blockIdMap[uploadUid] = new List<string>();
            }

            return blockIdMap;
        }

        /// <summary>
        /// 刪除暫存的chunk資料
        /// </summary>
        public static void CleanChunkData(string uploadUid)
        {
            blockIdMap = GetChunkData(uploadUid);

            if (blockIdMap.ContainsKey(uploadUid))
            {
                blockIdMap.Remove(uploadUid);
            }

            HttpContext.Current.Session.SetString(SESSION_CHUNK_KEY, Newtonsoft.Json.JsonConvert.SerializeObject(blockIdMap));
        }
    }
}
