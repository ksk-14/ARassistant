using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class AvatarManager : MonoBehaviour
{
	[SerializeField] GameObject _avatarGo;
	[SerializeField] GameObject _hackingGo;
	[SerializeField] Transform _playerTf;
	[SerializeField] AudioSource _worldAudioSrc;
	[SerializeField] AudioClip[] _firstReplyClips;
	[SerializeField] AudioClip _thikingClip;
	[SerializeField] AudioClip _hackingClip;

	private AudioSource _avatarAudioSrc;
	private Animator _avatarAnim;
	private Transform _avatarTf;
	private Vector3 _avatarInitPos;
	private SkinnedMeshRenderer _faceSkinnedMeshRenderer;

	// AudioSourceを元の設定値に戻す際に使用
	private AudioClip _oriWorldAudioClip;
	private bool _oriWorldAudioIsLoop;
	private float _oriWorldAudioVolume;

	// まばたきに関するメンバ
	private bool _isBlink = true;
	private int _eyeCloseIndex = 13;
	private int _faceAngryIndex = 2;
	private float _duration = 0.2f;

	private void Start()
	{
		this._avatarAudioSrc = this._avatarGo.GetComponent<AudioSource>();
		this._avatarAnim = this._avatarGo.GetComponent<Animator>();
		this._avatarTf = this._avatarGo.GetComponent<Transform>();
		this._avatarInitPos = this._avatarTf.transform.position;
		// ウインク関係
		this._faceSkinnedMeshRenderer = this._avatarTf.Find("Face").gameObject.GetComponent<SkinnedMeshRenderer>();
		StartCoroutine(this.BlinkMaster());
	}

	private IEnumerator BlinkMaster()
	{
		while (true)
		{
			yield return StartCoroutine(this.Blink(0.0f, this._duration));
			yield return new WaitForSeconds(Random.Range(3.0f, 10.0f));
			yield return StartCoroutine(this.Blink(100.0f, this._duration));
		}
	}

	private IEnumerator Blink(float targetWeight, float duration)
	{
		float time = 0;
		float startWeight = this._faceSkinnedMeshRenderer.GetBlendShapeWeight(this._eyeCloseIndex);
		while (time < duration)
		{
			if (this._isBlink)
			{
				float lerpWeight = Mathf.Lerp(startWeight, targetWeight, time / duration);
				this._faceSkinnedMeshRenderer.SetBlendShapeWeight(this._eyeCloseIndex, lerpWeight);
			}
			else
			{
				// まばたきをしない時は他の表情管理をしている時なので目を開かせておく
				this._faceSkinnedMeshRenderer.SetBlendShapeWeight(this._eyeCloseIndex, 0.0f);
			}
			time += Time.deltaTime;
			yield return null;
		}
		// 最後は確実に目的Weightに設定する
		this._faceSkinnedMeshRenderer.SetBlendShapeWeight(this._eyeCloseIndex, targetWeight);
			
	}

	/// <summary>アバターの最初の返事とアニメーションの設定</summary>
	public void AvatarReplySetting() 
	{
		// 最初の返事をランダムにセット
		this._avatarAudioSrc.clip = this._firstReplyClips[Random.Range(0, this._firstReplyClips.Length)];
		// アバターを音声入力中のアニメーションに変更
		this.SetTrigger("VoiceListen");
	}

	/// <summary>アバターをプレイヤーの方向に向けるコルーチン</summary>
	/// <param name="rotationDuration">プレイヤーの方を向くまでの時間</param>
	public IEnumerator AvatarLookPlayer(float duration)
	{
		// アバターから見たプレイヤーのベクトルを算出
		Vector3 directionToPlayer = this._playerTf.position - this._avatarTf.position;
		// ベクトルのY軸を無視することで、アバターがX軸回転しないようにする
		directionToPlayer.y = 0;

		//アバターの現在の回転を保存し、プレイヤーを向くための目標の回転を計算
		Quaternion startRotation = this._avatarTf.rotation;
		Quaternion endRotation = Quaternion.LookRotation(directionToPlayer);

		float elapsedTime = 0;

		// 経過時間に応じて、徐々にアバターをプレイヤーの方へ向かせる
		while (elapsedTime < duration)
		{
			this._avatarTf.rotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / duration);
			//this._avatarTf.position = Vector3.Lerp(this._avatarTf.position, this._avatarInitPos, elapsedTime / duration);
			elapsedTime += Time.deltaTime;
			//this._avatarTf.position = this._avatarInitPos;
			yield return null;
		}

		// 確実に目標の向きに設定
		this._avatarTf.rotation = endRotation;
	}

	/// <summary> アバターの最初の音声と、音声入力開始SEを再生する</summary>
	public IEnumerator PlayFirstReply()
	{
		// 最初の返事を再生し終えるまで待機
		this._avatarAudioSrc.Play();
		yield return new WaitWhile(() => this._avatarAudioSrc.isPlaying);
		yield return new WaitForSeconds(0.5f);
		yield return StartCoroutine(PlayInputStartSE());
	}

	/// <summary> 音声入力開始SEを再生する（フォローアップにも対応）</summary>
	public IEnumerator PlayInputStartSE(TaskCompletionSource<bool> tcs=null)
	{
		this._worldAudioSrc.Play();
		yield return new WaitWhile(() => this._worldAudioSrc.isPlaying);
		if (tcs != null) tcs.SetResult(true);
	}

	/// <summary>フォローアップ開始までの待機処理</summary>
	public IEnumerator WaitFollowUp(TaskCompletionSource<bool> tcs, int waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		tcs.SetResult(true);
	}

	/// <summary>クラウド処理中（シンキングタイム）の演出着火する</summary>
	public void StartThinking()
	{
		// アバターを考え中のアニメーションに変更
		this.SetTrigger("Thinking");
		this.SetWorldAudioSource(true, this._thikingClip, true, 0.2f);
		this._worldAudioSrc.Play();
	}

	/// <summary>クラウド処理中（シンキングタイム）の演出終了する</summary>
	public void StopThinking()
	{
		// アバターを考え中のアニメーションから戻す
		this.SetTrigger("ExitThinking");
		StartCoroutine(this.AvatarLookPlayer(0.2f));
		this._worldAudioSrc.Stop();
		this.SetWorldAudioSource(false);
	}

	/// <summary>S3からダウンロードした音声ファイルを読み込み、再生する</summary>
	/// <param name="wavFilePath">ローカルに保存された音声ファイルパス</param>
	public IEnumerator LoadAndPlayReply(string wavFilePath, TaskCompletionSource<bool> tcs)
	{
		// UnityWebRequestを使用して音声ファイルを読み込む（AudioType.WAV or AudioType.MPEG）
		using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + wavFilePath, AudioType.MPEG))
		{
			yield return www.SendWebRequest();

			if (www.result == UnityWebRequest.Result.Success)
			{
				// 音声ファイルの読み込みに成功した場合、AudioClipを作成し再生
				AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
				this._avatarAudioSrc.clip = clip;
				this._avatarAudioSrc.Play();
				yield return StartCoroutine(this.WaitPlayReplyComplete());
				tcs.SetResult(true);
			}
			else
			{
				Debug.LogError("ファイルの読み込みに失敗しました: " + www.error);
			}
		}
	}

	// 音声再生終了を待機するコルーチン
	private IEnumerator WaitPlayReplyComplete()
	{
		while (this._avatarAudioSrc.isPlaying)
		{
			yield return null;
		}
	}

	/// <summary>ハッキング演出を着火する（IoT機器操作時のみ呼ぶ）</summary>
	public IEnumerator StartHacking(TaskCompletionSource<bool> tcs, bool followUpFlg)
	{
		// アバターをハッキングのアニメーションに変更
		yield return StartCoroutine(this.AvatarLookPlayer(0.2f));
		this.SetTrigger("Hacking");
		this._isBlink = false;
		this._faceSkinnedMeshRenderer.SetBlendShapeWeight(this._faceAngryIndex, 100.0f);
		yield return new WaitForSeconds(0.8f);
		this.SetTrigger("Hacking_UI");
		this._hackingGo.SetActive(true);
		this.SetWorldAudioSource(true, this._hackingClip, false, 0.5f);
		this._worldAudioSrc.Play();
		yield return new WaitForSeconds(4.0f);
		this._isBlink = true;
		this._faceSkinnedMeshRenderer.SetBlendShapeWeight(this._faceAngryIndex, 0.0f);
		string triggerName = followUpFlg ? "FollowUpStart" : "ExitHacking";
		this.SetTrigger(triggerName);
		this._worldAudioSrc.Stop();
		this.SetWorldAudioSource(false);
		yield return new WaitForSeconds(0.5f);
		this._hackingGo.SetActive(false);
		tcs.SetResult(true);
	}

	/// <summary>アバターのアニメーショントリガーをセットする共通メソッド</summary>
	/// <param name="triggerName">トリガーパラメーター名</param>
	public void SetTrigger(string triggerName)
	{
		this._avatarAnim.SetTrigger(triggerName);
	}

	/// <summary>
	/// ワールドAudioSourceの状態設定メソッド
	/// 変更前の設定値を保存し、任意のタイミングで元に戻すことができる
	/// </summary>
	/// <param name="isSet">TRUE=新しく設定する, FALSE=元の状態に戻す</param>
	/// <param>他の引数は、設定を元に戻す時には不要なので非必須仕様</param>
	private void SetWorldAudioSource(bool isSet, AudioClip clip=null, bool isLoop=false, float volume=0.5f)
	{
		if (isSet)
		{
			this._oriWorldAudioClip = this._worldAudioSrc.clip;
			this._oriWorldAudioIsLoop = this._worldAudioSrc.loop;
			this._oriWorldAudioVolume = this._worldAudioSrc.volume;
			this._worldAudioSrc.clip = clip;
			this._worldAudioSrc.loop = isLoop;
			this._worldAudioSrc.volume = volume;
		}
		else
		{
			this._worldAudioSrc.clip = _oriWorldAudioClip;
			this._worldAudioSrc.loop = _oriWorldAudioIsLoop;
			this._worldAudioSrc.volume = _oriWorldAudioVolume;
		}
	}
}
