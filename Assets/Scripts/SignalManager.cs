using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;

public class SignalManager : MonoBehaviour
{
	[SerializeField] float _timeout = 30f;
	[SerializeField] float _pollInterval = 0.1f;

	private string _baseUrl;
	private string _pathPoll;
	private string _pathSendPlayed;
	private string _pathSendAnswer;

	private void Start()
	{
		// 環境設定ファイルから必要なデータを取得
		this._baseUrl = ConfigSingleton.Instance.Config.ServerURL;
		this._pathPoll = ConfigSingleton.Instance.Config.ServerPathPoll;
		this._pathSendPlayed = ConfigSingleton.Instance.Config.ServerPathSendPlayed;
		this._pathSendAnswer = ConfigSingleton.Instance.Config.ServerPathSendAnswer;
	}

	/// <summary>クラウド側での音声ファイル生成を検知するためにポーリングを行う</summary>
	public IEnumerator PollSignal(TaskCompletionSource<string> tcs)
	{
		float startTime = Time.time;

		while (Time.time - startTime < this._timeout)
		{
			using (UnityWebRequest request = UnityWebRequest.Get(this._baseUrl + this._pathPoll))
			{
				request.downloadHandler = new DownloadHandlerBuffer();
				yield return request.SendWebRequest();

				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError(request.error);
				} 
				else
				{
					string response = request.downloadHandler.text;
					Debug.Log(response);

					// JSONレスポンスを解析
					PollSignalStatus jsonResponse = JsonUtility.FromJson<PollSignalStatus>(response);
					if (jsonResponse.status == "fin")
					{
						tcs.SetResult("fin");
						break;
					}
					else if (jsonResponse.status == "fin_ask_again")
					{
						tcs.SetResult("fin_ask_again");
						break;
					}
				}
			}
			// 次のリクエストまで待機
			yield return new WaitForSeconds(this._pollInterval);
		}

		if (Time.time - startTime >= this._timeout){
			Debug.LogError("Timeout reached");
		}
	}

	/// <summary>クラウド側に音声ファイル再生終了を通知する</summary>
	public IEnumerator SendSignal(TaskCompletionSource<bool> tcs, string param) {
		// パラメータの有無によって送信先を変えている（パラメがない＝再生完了シグナル）
		string path = param == null ? this._pathSendPlayed : this._pathSendAnswer + param;
		using (UnityWebRequest webRequest = UnityWebRequest.Get(this._baseUrl + path)) {
			yield return webRequest.SendWebRequest();
			tcs.SetResult(true);
		}
	}
}

[System.Serializable]
public class PollSignalStatus
{
	public string status;
}


