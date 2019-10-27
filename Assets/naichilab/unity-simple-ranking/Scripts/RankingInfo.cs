using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naichilab
{
    /// <summary>
    /// １つのリーダーボード情報
    /// </summary>
    [CreateAssetMenu]
    public class RankingInfo : ScriptableObject
    {
        /// <summary>
        /// リーダーボード名
        /// </summary>
        public string BoardName = "ハイスコアランキング";

        /// <summary>
        /// クラス名（NCMBオブジェクト名として使われる）
        /// </summary>
        public string ClassName = "HiScore";

        /// <summary>
        /// スコアタイプ（数値 or 時間）
        /// </summary>
        public ScoreType Type;

        /// <summary>
        /// 並び順
        /// asc:昇順（小さい方が高スコア）
        /// desc:降順（大きい方が高スコア）
        /// </summary>
        [Tooltip("ASC:数値が小さい方がハイスコア、DESC:逆")] public ScoreOrder Order;

        /// <summary>
        /// 表示カスタムフォーマット
        /// </summary>
        public string CustomFormat;

        /// <summary>
        /// PlayFabのStatisticからIScoreを復元する
        /// </summary>
        /// <param name="statisticValue"></param>
        /// <returns></returns>
        public IScore BuildScore(int statisticValue)
        {
            //オーダーが昇順の場合はint.MaxValueから実データを引いてあげた値をサーバーに送っているので、逆算してあげる
            var realValue = Order == ScoreOrder.OrderByAscending ? int.MaxValue - statisticValue : statisticValue;
            try
            {
                switch (Type)
                {
                    case ScoreType.Number:
                        var d = realValue / (float)(1 << 16);
                        return new NumberScore(d, CustomFormat);
                        break;
                    case ScoreType.Time:
                        var t = new TimeSpan(realValue);
                        return new TimeScore(t, CustomFormat);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("不正なデータが渡されました。[" + statisticValue + "]");
            }

            return null;
        }

        /// <summary>
        /// PlayFabのStatisticに送るためintに変換してあげる
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public int ToStatisticValue(IScore score)
        {
            var statisticValue = Type == ScoreType.Number ? (int)(score.Value * (1 << 16)) : (int)score.Value;
            //オーダーが昇順の場合はint.MaxValueから実データを引いてあげた値をサーバーに送っているので、逆算してあげる
            return Order == ScoreOrder.OrderByAscending ? int.MaxValue - statisticValue : statisticValue;
        }
    }
}