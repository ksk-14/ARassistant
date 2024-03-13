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
		// ���ݒ�t�@�C������K�v�ȃf�[�^���擾
		this._baseUrl = ConfigSingleton.Instance.Config.ServerURL;
		this._pathPoll = ConfigSingleton.Instance.Config.ServerPathPoll;
		this._pathSendPlayed = ConfigSingleton.Instance.Config.ServerPathSendPlayed;
		this._pathSendAnswer = ConfigSingleton.Instance.Config.ServerPathSendAnswer;
	}

	/// <summary>�N���E�h���ł̉����t�@�C�����������m���邽�߂Ƀ|�[�����O���s��</summary>
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

					// JSON���X�|���X�����
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
			// ���̃��N�G�X�g�܂őҋ@
			yield return new WaitForSeconds(this._pollInterval);
		}

		if (Time.time - startTime >= this._timeout){
			Debug.LogError("Timeout reached");
		}
	}

	/// <summary>�N���E�h���ɉ����t�@�C���Đ��I����ʒm����</summary>
	public IEnumerator SendSignal(TaskCompletionSource<bool> tcs, string param) {
		// �p�����[�^�̗L���ɂ���đ��M���ς��Ă���i�p�������Ȃ����Đ������V�O�i���j
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


