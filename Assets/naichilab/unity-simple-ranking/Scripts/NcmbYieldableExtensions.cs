using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCMB.Extensions
{
/*    /// <summary>
    /// 待機可能NCMBQueryサブクラス
    /// ResultとErrorを外で用意するのが面倒なので作成。ついでにCountメソッド追加済み
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class YieldableNcmbQuery<T> : NCMBQuery<T> where T : NCMBObject
    {
        public List<T> Result { private set; get; }
        public NCMBException Error { private set; get; }

        public int Count
        {
            get { return Result == null ? 0 : Result.Count; }
        }

        public CustomYieldInstruction FindAsync()
        {
            Result = null;
            Error = null;
            FindAsync((objects, error) =>
            {
                Result = objects;
                Error = error;
            });
            return new WaitWhile(() => Result == null && Error == null);
        }

        public YieldableNcmbQuery(string theClassName) : base(theClassName)
        {
        }
    }

    public static class NCMBExtensions
    {
        /// <summary>
        /// yield return で待機可能なFindAsync
        /// </summary>
        /// <param name="ncmbquery"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static CustomYieldInstruction YieldableFindAsync<T>(this NCMBQuery<T> ncmbquery, NCMBQueryCallback<T> callback = null) where T : NCMBObject
        {
            var isComplete = false;
            ncmbquery.FindAsync((objects, error) =>
            {
                if (callback != null) callback(objects, error);
                isComplete = true;
            });
            return new WaitUntil(() => isComplete);
        }

        /// <summary>
        /// yield return で待機可能なSaveAsync
        /// </summary>
        /// <param name="ncmbobj"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static CustomYieldInstruction YieldableSaveAsync(this NCMBObject ncmbobj, NCMBCallback callback = null)
        {
            var isComplete = false;
            ncmbobj.SaveAsync(error =>
            {
                if (callback != null) callback(error);
                isComplete = true;
            });
            return new WaitUntil(() => isComplete);
        }
    }
    */
}

namespace PlayFab.extensions
{

    public static class NCMBExtensions
    {
        public static CustomYieldInstruction ToYieldable<TSuccess, TError>(Action<Action<TSuccess>, Action<TError>> callbackregist)
        {
            var isComplete = false;
            Action<TSuccess> resolve = suc => { isComplete = true; };
            Action<TError> reject = suc => { isComplete = true; };
            callbackregist(resolve, reject);
            return new WaitUntil(() => isComplete);
        }
    }

    public class YieldablePromise<TSuccess,TError> : CustomYieldInstruction
    {
        public delegate void PromiseResolveReject(Action<TSuccess> resolve, Action<TError> reject);

        public TSuccess Result { private set; get; }
        public TError Error { private set; get; }

        private bool isComplete = false;

        public YieldablePromise(PromiseResolveReject callbackregist)
        {
            callbackregist(success =>
            {
                Result = success;
                isComplete = true;
            }, error =>
            {
                Error = error;
                isComplete = true;
            });
        }

        public override bool keepWaiting => !isComplete;
    }

    /// <summary>
    /// PlayFabAPIはError時にはPlayFabErrorが返却されるようなので、定義
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    public class YieldablePlayfabPromise<TSuccess> : YieldablePromise<TSuccess,PlayFabError>
    {
        public YieldablePlayfabPromise(PromiseResolveReject callbackregist) : base(callbackregist)
        {
        }
    }

}