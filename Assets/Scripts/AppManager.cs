using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// このクラスはアプリ全体の実行順序を管理するためのクラス
/// 一部、下記条件分岐も行っている
/// IoT操作・情報要求・アバターのフォローアップ応答・フォローアップに対するユーザーのアンサー
/// </summary>
/// <remarks>
/// 外部メソッドやコルーチンの役割はsummaryに記載しているので、IDEでフォーカスして処理内容を確認する
/// </remarks>
public class AppManager : MonoBehaviour
{
	[Header("Manager Reference")]
	[SerializeField] AvatarManager _avatarMg;
	[SerializeField] VoiceManager _voiceMg;
	[SerializeField] S3UploadManager _s3UpMg;
	[SerializeField] S3DownloadManager _s3DlMg;
	[SerializeField] SignalManager _signalMg;

	private FollowUpSingleton _followUpIns;
	private bool _isIoT = false;

	private void Start()
	{
		this._followUpIns = FollowUpSingleton.Instance;
	}

	// ===============================================================
	// ハンドジェスチャー検知時のイベントハンドラー
	public void OnHandPose()
	{
		StartCoroutine(this.HandPoseHandler());
	}

	// ハンドジェスチャー検知時に呼ばれる
	private IEnumerator HandPoseHandler()
	{
		this.ResetFollowUpState();
		this._avatarMg.AvatarReplySetting();
		yield return StartCoroutine(this._avatarMg.AvatarLookPlayer(0.7f));
		yield return StartCoroutine(this._avatarMg.PlayFirstReply());
		this._voiceMg.VoiceInputStart();
	}

	// フォローアップステートをリセット（前セッションのステートが残っている可能性があるため）
	public void ResetFollowUpState()
	{
		this._followUpIns.questionPath = null;
		this._followUpIns.yesPath = null;
		this._followUpIns.noPath = null;
		this._followUpIns.afterPath = null;
	}
	// ===============================================================
	// WitAIの音声解析完了後に着火（情報要求インテント）
	public async void OnVoiceInfo(string[] analysisData)
	{
		this._isIoT = false;
		string intent = "Info";
		await this.OnVoiceAnalysis(analysisData, intent);
	}

	// WitAIの音声解析完了後に着火（IoT指示インテント）
	public async void OnVoiceSwitchBot(string[] analysisData)
	{
		this._isIoT = true;
		string intent = "SwitchBot";
		await this.OnVoiceAnalysis(analysisData, intent);
	}

	// WitAIの音声解析完了後に着火（フォローアップアンサーインテント）
	public async void OnVoiceAnswer(string[] analysisData)
	{
		this._isIoT = analysisData[0] == "Yes" ? true : false;
		string folllowUpVoice = analysisData[0] == "Yes" ? this._followUpIns.yesPath : this._followUpIns.noPath;

		await this.CommonAfterHandler(folllowUpVoice, analysisData[0].ToLower());

		// フォローアップをする場合は、アフターボイスも再生する
		if (analysisData[0] == "Yes")
		{
			await this.CallWaitFollowUpCoroutine(2);
			await this.CallPlayReplyCoroutine(this._followUpIns.afterPath);
		}
	}

	// ===============================================================
	// メイン処理ハンドラー
	private async Task OnVoiceAnalysis(string[] analysisData, string intent)
	{
		var jsonData = this._voiceMg.MakeVoiceJson(analysisData, intent);
		this._avatarMg.StartThinking();
		await this._s3UpMg.S3UploadAsync(jsonData);
		string cloudStatus = await this.CallPollSignalCoroutine();
		string wavFilePath = await this._s3DlMg.DownloadWavAsync();
		this._avatarMg.StopThinking();
		// フォローアップの有無によって処理を切り替える
		if (cloudStatus == "fin_ask_again")
		{
			await this.FollowUpAfterHandler(wavFilePath);
		}
		else
		{
			await this.CommonAfterHandler(wavFilePath);
		}
	}

	// クラウド連携後の後処理（全ケース共通）
	private async Task CommonAfterHandler(string wavFilePath, string param=null, bool followUpFlg=false)
	{
		await this.CallPlayReplyCoroutine(wavFilePath);
		// 今セッションのIoT操作有無によって処理を分岐
		if (this._isIoT) {
			this.CallSendSignalCoroutine(param);
			await this.CallHackingAnimCoroutine(followUpFlg);
		}
		else
		{
			this._avatarMg.SetTrigger("ExitReply");
			await this.CallSendSignalCoroutine(param);
		}
	}

	// クラウド連携後の後処理（フォローアップがある場合）
	private async Task FollowUpAfterHandler(string wavFilePath)
	{
		// バックグラウンドでフォローアップの準備を進めておく
		this.FollowUpSetAsync();
		await this.CommonAfterHandler(wavFilePath, null, true);
		// フォローアップの準備が完了している場合はフォローアップ開始
		if (await this.CheckAllFollowUpSetPath())
		{
			await this.CallWaitFollowUpCoroutine(2);
			await this.CallPlayReplyCoroutine(this._followUpIns.questionPath);
			await this.CallPlayInputStartSECoroutine();
			this._voiceMg.VoiceInputStart();
		}
	}

	// フォローアップに必要な音声データを取得
	private async void FollowUpSetAsync()
	{
		await this.CallPollSignalCoroutine();
		bool followUpFlg = true;
		await this._s3DlMg.DownloadWavAsync(followUpFlg);
	}

	// フォローアップに必要な音声データが全て準備できているかチェック
	private async Task<Boolean> CheckAllFollowUpSetPath()
	{
		return this._followUpIns.questionPath != null &&
					this._followUpIns.yesPath != null &&
					this._followUpIns.noPath != null &&
					this._followUpIns.afterPath != null;
	}

	// ===============================================================
	/// <summary>
	/// クラウドでの音声ファイル生成検知コルーチンを着火
	/// </summary>
	/// <returns>ポーリングで得られたステータス（フォローアップが必要か判断するのに使用）</returns>
	private async Task<string> CallPollSignalCoroutine()
	{
		TaskCompletionSource<string>  pollTcs = new TaskCompletionSource<string>();
		StartCoroutine(this._signalMg.PollSignal(pollTcs));
		string cloudStatus = await pollTcs.Task;
		return cloudStatus;
	}

	/// <summary>
	/// クラウドで作成された音声ファイルを再生するコルーチンを着火
	/// </summary>
	/// <param name="wavFilePath">音声ファイル</param>
	private async Task CallPlayReplyCoroutine(string wavFilePath)
	{
		var replyTcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._avatarMg.LoadAndPlayReply(wavFilePath, replyTcs));
		await replyTcs.Task;
	}

	/// <summary>
	/// 音声再生が完了したことをクラウドに通知するコルーチン着火
	/// </summary>
	/// <param name="param">エンドポイントのパラメータ（フォローアップ時のみ指定）</param>
	private async Task CallSendSignalCoroutine(string param)
	{
		var sendTcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._signalMg.SendSignal(sendTcs, param));
		await sendTcs.Task;
	}

	/// <summary>
	/// ハッキングアニメーションコルーチンを着火
	/// </summary>
	/// <param name="followUpFlg">フォローアップフラグ</param>
	private async Task CallHackingAnimCoroutine(bool followUpFlg)
	{
		var HackingAnimTcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._avatarMg.StartHacking(HackingAnimTcs, followUpFlg));
		await HackingAnimTcs.Task;
	}

	/// <summary>
	/// 音声入力開始SE再生コルーチンを着火
	/// </summary>
	private async Task CallPlayInputStartSECoroutine()
	{
		var InputStartSETcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._avatarMg.PlayInputStartSE(InputStartSETcs));
		await InputStartSETcs.Task;
	}

	/// <summary>
	/// フォローアップ質問までの待機コルーチンを着火
	/// </summary>
	/// <param name="waitTime">待機時間</param>
	private async Task CallWaitFollowUpCoroutine(int waitTime)
	{
		var wiatFollowUpTcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._avatarMg.WaitFollowUp(wiatFollowUpTcs, waitTime));
		await wiatFollowUpTcs.Task;
	}
}