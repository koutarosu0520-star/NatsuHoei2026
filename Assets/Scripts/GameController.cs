using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    // -------------------------------------------------------
    /// <summary>
    /// ゲームステート.
    /// </summary>
    // -------------------------------------------------------
    public enum PlayState
    {
        None,
        Ready,
        Play,
        Finish,
    }

    // 現在のステート.
    public PlayState CurrentState = PlayState.None;

    //! カウントダウンスタートタイム.
    [SerializeField] int countStartTime = 5;

    // ゲームの制限時間（秒）
    [SerializeField] float timeLimit = 60.0f;

    //! カウントダウンテキスト.
    [SerializeField] Text countdownText = null;
    //! タイマーテキスト.
    [SerializeField] Text timerText = null;
    
    // 【変更】タイムアップ時に表示するパネル（GameObjectに変更）
    [SerializeField] GameObject timeUpPanel = null;

    // カウントダウンの現在値.
    float currentCountDown = 0;
    // ゲーム経過時間現在値.
    float timer = 0;

    void Start()
    {
        // 【変更】ゲーム開始時はタイムアップのパネルを非表示にしておく
        if (timeUpPanel != null)
        {
            timeUpPanel.SetActive(false);
        }

        CountDownStart();
    }

    void Update()
    {
        // ステートがReadyのとき.
        if( CurrentState == PlayState.Ready )
        {
            currentCountDown -= Time.deltaTime;

            int intNum = 0;
            if ( currentCountDown <= (float)countStartTime && currentCountDown > 0 )
            {
                intNum = (int)Mathf.Ceil( currentCountDown );
                countdownText.text = intNum.ToString();
            }
            else if( currentCountDown <= 0 )
            {
                StartPlay();
                intNum = 0;
                countdownText.text = "Start!!";

                StartCoroutine( WaitErase() );
            }

            if(timerText != null) timerText.text = "Time : " + timer.ToString( "000.0" ) + " s";
        }
        // ステートがPlayのとき.
        else if( CurrentState == PlayState.Play )
        {
            timer -= Time.deltaTime;

            // 0秒以下になったら終了処理
            if (timer <= 0)
            {
                timer = 0;
                SetPlayState(PlayState.Finish);
                
                // 【変更】タイムアップのパネルを表示し、最前面に移動させる
                if (timeUpPanel != null)
                {
                    timeUpPanel.SetActive(true);
                    timeUpPanel.transform.SetAsLastSibling(); // これが最前面に出す魔法のコードです
                }
                Time.timeScale = 0f; 
                
                Debug.Log("Time Up!!");
            }

            if(timerText != null) timerText.text = "Time : " + timer.ToString( "000.0" ) + " s";
        }
        else
        {
            if(timerText != null) timerText.text = "Time : " + timer.ToString( "000.0" ) + " s";
        }
    }

    // -------------------------------------------------------
    /// <summary>
    /// カウントダウンスタート.
    /// </summary>
    // -------------------------------------------------------
    void CountDownStart()
    {
        currentCountDown = (float)countStartTime;
        timer = timeLimit;

        SetPlayState( PlayState.Ready );
        countdownText.gameObject.SetActive( true );
    }

    void StartPlay()
    {
        Debug.Log( "Start!!!" );
        SetPlayState( PlayState.Play );
    }

    IEnumerator WaitErase()
    {
        yield return new WaitForSeconds( 0.5f );
        countdownText.gameObject.SetActive( false );
    }

    void SetPlayState( PlayState state )
    {
        CurrentState = state;
    }
}