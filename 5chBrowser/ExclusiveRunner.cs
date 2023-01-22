using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _5chBrowser
{
    public static class ExclusiveRunner
    {
        private static Dictionary<string, object> syncObjectList = new();

        // 同IDを排他実行
        public static async Task<T> Run<T>(Func<Task<T>> func, string id)
        {
            var syncObject = GetSyncObject(id);
            return await Task.Run(() =>
            {
                lock (syncObject)
                    return func().Result;
            });
        }

        public static async Task Run(Func<Task> func, string id)
        {
            var syncObject = GetSyncObject(id);
            await Task.Run(() =>
            {
                lock (syncObject)
                    return func();
            });
        }

        public static T Run<T>(Func<T> func, string id)
        {
            var syncObject = GetSyncObject(id);
            lock (syncObject)
                return func();
        }

        public static void Run(Action func, string id)
        {
            var syncObject = GetSyncObject(id);
            lock (syncObject)
                func();
        }

        private static object GetSyncObject(string id)
        {
            lock (syncObjectList)
            {
                var syncObject = syncObjectList.FirstOrDefault(item => item.Key == id).Value ?? (syncObjectList[id] = new());

                syncObjectList.Remove(id);
                syncObjectList.Add(id, syncObject);

                if (syncObjectList.Count > 200)
                    syncObjectList.Remove(syncObjectList.First().Key);

                return syncObject;
            }
        }
    }
}
