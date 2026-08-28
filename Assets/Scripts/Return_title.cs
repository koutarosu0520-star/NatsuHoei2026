using UnityEngine;
using UnityEngine.SceneManagement;

// 【修正1】Unityのオブジェクトに付けて使うには「: MonoBehaviour」が必要です
public class Return_Title : MonoBehaviour
{
    // 【修正2】ボタンから呼び出す（外部からアクセスする）には「public」が必要です
    public void GoToTitle()
    {
        // 【超重要】ポーズ画面からタイトルに戻る場合、止まった時間を元に戻さないと、タイトル画面もフリーズしてしまいます！
        Time.timeScale = 1f; 

        SceneManager.LoadScene("TitleScene");
    }
}