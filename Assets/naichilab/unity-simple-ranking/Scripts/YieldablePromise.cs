using System;
using UnityEngine;

namespace PlayFab.extensions
{
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
    public class YieldablePlayfabPromise<TSuccess> : YieldablePromise<TSuccess, PlayFabError>
    {
        public YieldablePlayfabPromise(PromiseResolveReject callbackregist) : base(callbackregist)
        {
        }
    }
}