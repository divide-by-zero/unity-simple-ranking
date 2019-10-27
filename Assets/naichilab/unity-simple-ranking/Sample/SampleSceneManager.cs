using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SampleSceneManager : MonoBehaviour
{
    public Text scoreText;
    [NonSerialized] float score = 0;

    public Text stopWatchText;
    [NonSerialized] TimeSpan time;
    private bool isStart;

    public void OnPlusButtonPressed()
    {
        score+=0.1f;
        scoreText.text = score.ToString();
    }

    public void OnMinusButtonPressed()
    {
        score-=0.1f;
        scoreText.text = score.ToString();
    }

    public void OnResultButton0Pressed()
    {
        naichilab.RankingLoader.Instance.SendScoreAndShowRanking(score, 0);
    }

    public void OnResultButton1Pressed()
    {
        naichilab.RankingLoader.Instance.SendScoreAndShowRanking(score, 1);
    }

    public void OnResultButton2Pressed()
    {
        naichilab.RankingLoader.Instance.SendScoreAndShowRanking(time, 2);
    }

    public void OnStopWatchButtonPressed()
    {
        isStart = !isStart;
    }

    public void LocalSaveReset()
    {
        score = 0;
        time = TimeSpan.Zero;
        scoreText.text = score.ToString();
        PlayerPrefs.DeleteAll();
    }

    private void Update()
    {
        if (isStart)
        {
            time+= TimeSpan.FromSeconds(Time.deltaTime);
            stopWatchText.text = time.ToString("mm':'ss'.'ff");
        }
    }
}