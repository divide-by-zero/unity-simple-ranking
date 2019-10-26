using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using NCMB;
using NCMB.Extensions;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.extensions;
using UnityEditor;

namespace naichilab
{
    public class RankingSceneManager : MonoBehaviour
    {
        private const string OBJECT_ID = "objectId";
        private const string COLUMN_SCORE = "score";
        private const string COLUMN_NAME = "name";


        [SerializeField] Text captionLabel;
        [SerializeField] Text scoreLabel;
        [SerializeField] Text highScoreLabel;
        [SerializeField] InputField nameInputField;
        [SerializeField] Button sendScoreButton;
        [SerializeField] Button closeButton;
        [SerializeField] RectTransform scrollViewContent;
        [SerializeField] GameObject rankingNodePrefab;
        [SerializeField] GameObject readingNodePrefab;
        [SerializeField] GameObject notFoundNodePrefab;
        [SerializeField] GameObject unavailableNodePrefab;

        private string _objectid = null;

        private string ObjectID
        {
            get { return _objectid ?? (_objectid = PlayerPrefs.GetString(BoardIdPlayerPrefsKey, null)); }
            set
            {
                if (_objectid == value)
                    return;
                PlayerPrefs.SetString(BoardIdPlayerPrefsKey, _objectid = value);
            }
        }

        private string BoardIdPlayerPrefsKey
        {
            get { return string.Format("{0}_{1}_{2}", "board", _board.ClassName, OBJECT_ID); }
        }

        private RankingInfo _board;
        private IScore _lastScore;

//        private NCMBObject _ncmbRecord;

        /// <summary>
        /// 入力した名前
        /// </summary>
        /// <value>The name of the inputted.</value>
        private string InputtedNameForSave
        {
            get
            {
                if (string.IsNullOrEmpty(nameInputField.text))
                {
                    return "名無し";
                }

                return nameInputField.text;
            }
        }

        void Start()
        {
            sendScoreButton.interactable = false;
            _board = RankingLoader.Instance.CurrentRanking;
            _lastScore = RankingLoader.Instance.LastScore;

            Debug.Log(BoardIdPlayerPrefsKey + "=" + PlayerPrefs.GetString(BoardIdPlayerPrefsKey, null));

            StartCoroutine(GetHighScoreAndRankingBoard());
        }

        IEnumerator GetHighScoreAndRankingBoard()
        {
            scoreLabel.text = _lastScore.TextForDisplay;
            captionLabel.text = string.Format("{0}ランキング", _board.BoardName);
            yield return null;
            //ハイスコア取得
            {
                highScoreLabel.text = "取得中...";

                //まずID作成
                var id = ObjectID;
                if (string.IsNullOrWhiteSpace(id)) ObjectID = GUID.Generate().ToString();

                var loginWithCustomIdRequest = new LoginWithCustomIDRequest() { CustomId = ObjectID, CreateAccount = true ,InfoRequestParameters = new GetPlayerCombinedInfoRequestParams()
                {
                    GetPlayerProfile = true,
                }};
                var loginPromise = new YieldablePromise<LoginResult, PlayFabError>((resolve, reject) => PlayFabClientAPI.LoginWithCustomID(loginWithCustomIdRequest, resolve, reject));
                yield return loginPromise;

                if (loginPromise.Error != null)
                {
                    //TODO なんかエラーメッセージを画面に出すk？かな？
                    Debug.Log(loginPromise.Error.ErrorMessage);
                    yield break;
                }
                else
                {
                    Debug.Log(loginPromise.Result.LastLoginTime);
                }

                var hiscoreDataPromise = new YieldablePromise<GetPlayerStatisticsResult,PlayFabError>((resolve, reject) => PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), resolve, reject));
                yield return hiscoreDataPromise;

                //TODO 本当はここでユーザーがハイスコアを登録済みかどうかを知りたい。　どうやってとるのかわからんけどもね。
                

                //TODO てすと
                PlayFabClientAPI.GetUserData(new GetUserDataRequest()
                {
                    PlayFabId = ""//TODO ここで他ユーザーも指定・・・？
                }, result => { }, error => { });
                //TODO


                //全てのstatisticsが手に入っちゃうかもね
                Debug.Log(hiscoreDataPromise.Result.Statistics.Count);
                var hiscore = hiscoreDataPromise.Result.Statistics.FirstOrDefault(value => value.StatisticName == _board.ClassName);
                if (hiscore != null)
                {
                    //既にハイスコアは登録されている
                    var s = _board.BuildScore(hiscore.Value.ToString());
                    highScoreLabel.text = s != null ? s.TextForDisplay : "エラー";

                    nameInputField.text = loginPromise.Result.InfoResultPayload.PlayerProfile.DisplayName;
                }
                else
                {
                    //登録されていない
                    highScoreLabel.text = "-----";
                }
            }

            //ランキング取得
            yield return StartCoroutine(LoadRankingBoard());

            sendScoreButton.interactable = true;


            //            //スコア更新している場合、ボタン有効化
            //            if (_ncmbRecord == null)
            //            {
            //                sendScoreButton.interactable = true;
            //            }
            //            else
            //            {
            //                var highScore = _board.BuildScore(_ncmbRecord[COLUMN_SCORE].ToString());
            //
            //                if (_board.Order == ScoreOrder.OrderByAscending)
            //                {
            //                    //数値が低い方が高スコア
            //                    sendScoreButton.interactable = _lastScore.Value < highScore.Value;
            //                }
            //                else
            //                {
            //                    //数値が高い方が高スコア
            //                    sendScoreButton.interactable = highScore.Value < _lastScore.Value;
            //                }
            //
            //                Debug.Log(string.Format("登録済みスコア:{0} 今回スコア:{1} ハイスコア更新:{2}", highScore.Value, _lastScore.Value,sendScoreButton.interactable));
            //            }
        }


        public void SendScore()
        {
            StartCoroutine(SendScoreEnumerator());
        }

        private IEnumerator SendScoreEnumerator()
        {
            sendScoreButton.interactable = false;
            highScoreLabel.text = "送信中...";

            var statisticUpdate = new StatisticUpdate
            {
                // 統計情報名を指定します。
                StatisticName = _board.ClassName,
                Value = (int)_lastScore.Value,
            };

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    statisticUpdate
                }
            };

            var sendScore =  new YieldablePlayfabPromise<UpdatePlayerStatisticsResult>((OnSuccess,OnError)=>PlayFabClientAPI.UpdatePlayerStatistics(request, OnSuccess, OnError));
            yield return sendScore;

//            _ncmbRecord[COLUMN_NAME] = InputtedNameForSave;   //TODO DisplayName




            if (sendScore.Error != null)
            {
                //NCMBのコンソールから直接削除した場合に、該当のobjectIdが無いので発生する（らしい）
                //TODO なんかエラー
                yield break;
            }

            highScoreLabel.text = _lastScore.TextForDisplay;

            yield return StartCoroutine(LoadRankingBoard());
        }


        /// <summary>
        /// ランキング取得＆表示
        /// </summary>
        /// <returns>The ranking board.</returns>
        private IEnumerator LoadRankingBoard()
        {
            int nodeCount = scrollViewContent.childCount;
            for (int i = nodeCount - 1; i >= 0; i--)
            {
                Destroy(scrollViewContent.GetChild(i).gameObject);
            }

            var msg = Instantiate(readingNodePrefab, scrollViewContent);

            //2017.2.0b3の描画されないバグ暫定対応
            MaskOffOn();

            //これを指定することで、ランキングで得られる情報が増える（らしい？）
            var playerProfileViewConstraints = new PlayerProfileViewConstraints()
            {
                ShowDisplayName = true,
                ShowStatistics = true
            };

            var request = new GetLeaderboardRequest
            {
                StatisticName = _board.ClassName, // 統計情報名を指定します。
                StartPosition = 0, // 何位以降のランキングを取得するか指定します。
                MaxResultsCount = 30, // ランキングデータを何件取得するか指定します。最大が100です。
                ProfileConstraints = playerProfileViewConstraints
            };

            var so = new YieldablePromise<GetLeaderboardResult, PlayFabError>((resolve, reject) => PlayFabClientAPI.GetLeaderboard(request, resolve, reject));
            yield return so;

            Debug.Log("データ取得 : " + so.Result.Leaderboard.Count.ToString() + "件");
            Destroy(msg);

            if (so.Error != null)
            {
                Instantiate(unavailableNodePrefab, scrollViewContent);
            }
            else if (so.Result.Leaderboard.Count > 0)
            {
                int rank = 0;
                foreach (var r in so.Result.Leaderboard)
                {
                    var n = Instantiate(rankingNodePrefab, scrollViewContent);
                    var rankNode = n.GetComponent<RankingNode>();
                    rankNode.NoText.text = (++rank).ToString();
                    rankNode.NameText.text = r.DisplayName;

                    var s = _board.BuildScore(r.StatValue.ToString());
                    rankNode.ScoreText.text = s != null ? s.TextForDisplay : "エラー";

    //                    Debug.Log(r[COLUMN_SCORE].ToString());
                }
            }
            else
            {
                Instantiate(notFoundNodePrefab, scrollViewContent);
            }
        }

        public void OnCloseButtonClick()
        {
            closeButton.interactable = false;
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Ranking");
        }

        private void MaskOffOn()
        {
            //2017.2.0b3でなぜかScrollViewContentを追加しても描画されない場合がある。
            //親maskをOFF/ONすると直るので無理やり・・・
            var m = scrollViewContent.parent.GetComponent<Mask>();
            m.enabled = false;
            m.enabled = true;
        }
    }
}