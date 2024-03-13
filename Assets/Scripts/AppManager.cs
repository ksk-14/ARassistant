using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ���̃N���X�̓A�v���S�̂̎��s�������Ǘ����邽�߂̃N���X
/// �ꕔ�A���L����������s���Ă���
/// IoT����E���v���E�A�o�^�[�̃t�H���[�A�b�v�����E�t�H���[�A�b�v�ɑ΂��郆�[�U�[�̃A���T�[
/// </summary>
/// <remarks>
/// �O�����\�b�h��R���[�`���̖�����summary�ɋL�ڂ��Ă���̂ŁAIDE�Ńt�H�[�J�X���ď������e���m�F����
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
	// �n���h�W�F�X�`���[���m���̃C�x���g�n���h���[
	public void OnHandPose()
	{
		StartCoroutine(this.HandPoseHandler());
	}

	// �n���h�W�F�X�`���[���m���ɌĂ΂��
	private IEnumerator HandPoseHandler()
	{
		this.ResetFollowUpState();
		this._avatarMg.AvatarReplySetting();
		yield return StartCoroutine(this._avatarMg.AvatarLookPlayer(0.7f));
		yield return StartCoroutine(this._avatarMg.PlayFirstReply());
		this._voiceMg.VoiceInputStart();
	}

	// �t�H���[�A�b�v�X�e�[�g�����Z�b�g�i�O�Z�b�V�����̃X�e�[�g���c���Ă���\�������邽�߁j
	public void ResetFollowUpState()
	{
		this._followUpIns.questionPath = null;
		this._followUpIns.yesPath = null;
		this._followUpIns.noPath = null;
		this._followUpIns.afterPath = null;
	}
	// ===============================================================
	// WitAI�̉�����͊�����ɒ��΁i���v���C���e���g�j
	public async void OnVoiceInfo(string[] analysisData)
	{
		this._isIoT = false;
		string intent = "Info";
		await this.OnVoiceAnalysis(analysisData, intent);
	}

	// WitAI�̉�����͊�����ɒ��΁iIoT�w���C���e���g�j
	public async void OnVoiceSwitchBot(string[] analysisData)
	{
		this._isIoT = true;
		string intent = "SwitchBot";
		await this.OnVoiceAnalysis(analysisData, intent);
	}

	// WitAI�̉�����͊�����ɒ��΁i�t�H���[�A�b�v�A���T�[�C���e���g�j
	public async void OnVoiceAnswer(string[] analysisData)
	{
		this._isIoT = analysisData[0] == "Yes" ? true : false;
		string folllowUpVoice = analysisData[0] == "Yes" ? this._followUpIns.yesPath : this._followUpIns.noPath;

		await this.CommonAfterHandler(folllowUpVoice, analysisData[0].ToLower());

		// �t�H���[�A�b�v������ꍇ�́A�A�t�^�[�{�C�X���Đ�����
		if (analysisData[0] == "Yes")
		{
			await this.CallWaitFollowUpCoroutine(2);
			await this.CallPlayReplyCoroutine(this._followUpIns.afterPath);
		}
	}

	// ===============================================================
	// ���C�������n���h���[
	private async Task OnVoiceAnalysis(string[] analysisData, string intent)
	{
		var jsonData = this._voiceMg.MakeVoiceJson(analysisData, intent);
		this._avatarMg.StartThinking();
		await this._s3UpMg.S3UploadAsync(jsonData);
		string cloudStatus = await this.CallPollSignalCoroutine();
		string wavFilePath = await this._s3DlMg.DownloadWavAsync();
		this._avatarMg.StopThinking();
		// �t�H���[�A�b�v�̗L���ɂ���ď�����؂�ւ���
		if (cloudStatus == "fin_ask_again")
		{
			await this.FollowUpAfterHandler(wavFilePath);
		}
		else
		{
			await this.CommonAfterHandler(wavFilePath);
		}
	}

	// �N���E�h�A�g��̌㏈���i�S�P�[�X���ʁj
	private async Task CommonAfterHandler(string wavFilePath, string param=null, bool followUpFlg=false)
	{
		await this.CallPlayReplyCoroutine(wavFilePath);
		// ���Z�b�V������IoT����L���ɂ���ď����𕪊�
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

	// �N���E�h�A�g��̌㏈���i�t�H���[�A�b�v������ꍇ�j
	private async Task FollowUpAfterHandler(string wavFilePath)
	{
		// �o�b�N�O���E���h�Ńt�H���[�A�b�v�̏�����i�߂Ă���
		this.FollowUpSetAsync();
		await this.CommonAfterHandler(wavFilePath, null, true);
		// �t�H���[�A�b�v�̏������������Ă���ꍇ�̓t�H���[�A�b�v�J�n
		if (await this.CheckAllFollowUpSetPath())
		{
			await this.CallWaitFollowUpCoroutine(2);
			await this.CallPlayReplyCoroutine(this._followUpIns.questionPath);
			await this.CallPlayInputStartSECoroutine();
			this._voiceMg.VoiceInputStart();
		}
	}

	// �t�H���[�A�b�v�ɕK�v�ȉ����f�[�^���擾
	private async void FollowUpSetAsync()
	{
		await this.CallPollSignalCoroutine();
		bool followUpFlg = true;
		await this._s3DlMg.DownloadWavAsync(followUpFlg);
	}

	// �t�H���[�A�b�v�ɕK�v�ȉ����f�[�^���S�ď����ł��Ă��邩�`�F�b�N
	private async Task<Boolean> CheckAllFollowUpSetPath()
	{
		return this._followUpIns.questionPath != null &&
					this._followUpIns.yesPath != null &&
					this._followUpIns.noPath != null &&
					this._followUpIns.afterPath != null;
	}

	// ===============================================================
	/// <summary>
	/// �N���E�h�ł̉����t�@�C���������m�R���[�`���𒅉�
	/// </summary>
	/// <returns>�|�[�����O�œ���ꂽ�X�e�[�^�X�i�t�H���[�A�b�v���K�v�����f����̂Ɏg�p�j</returns>
	private async Task<string> CallPollSignalCoroutine()
	{
		TaskCompletionSource<string>  pollTcs = new TaskCompletionSource<string>();
		StartCoroutine(this._signalMg.PollSignal(pollTcs));
		string cloudStatus = await pollTcs.Task;
		return cloudStatus;
	}

	/// <summary>
	/// �N���E�h�ō쐬���ꂽ�����t�@�C�����Đ�����R���[�`���𒅉�
	/// </summary>
	/// <param name="wavFilePath">�����t�@�C��</param>
	private async Task CallPlayReplyCoroutine(string wavFilePath)
	{
		var replyTcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._avatarMg.LoadAndPlayReply(wavFilePath, replyTcs));
		await replyTcs.Task;
	}

	/// <summary>
	/// �����Đ��������������Ƃ��N���E�h�ɒʒm����R���[�`������
	/// </summary>
	/// <param name="param">�G���h�|�C���g�̃p�����[�^�i�t�H���[�A�b�v���̂ݎw��j</param>
	private async Task CallSendSignalCoroutine(string param)
	{
		var sendTcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._signalMg.SendSignal(sendTcs, param));
		await sendTcs.Task;
	}

	/// <summary>
	/// �n�b�L���O�A�j���[�V�����R���[�`���𒅉�
	/// </summary>
	/// <param name="followUpFlg">�t�H���[�A�b�v�t���O</param>
	private async Task CallHackingAnimCoroutine(bool followUpFlg)
	{
		var HackingAnimTcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._avatarMg.StartHacking(HackingAnimTcs, followUpFlg));
		await HackingAnimTcs.Task;
	}

	/// <summary>
	/// �������͊J�nSE�Đ��R���[�`���𒅉�
	/// </summary>
	private async Task CallPlayInputStartSECoroutine()
	{
		var InputStartSETcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._avatarMg.PlayInputStartSE(InputStartSETcs));
		await InputStartSETcs.Task;
	}

	/// <summary>
	/// �t�H���[�A�b�v����܂ł̑ҋ@�R���[�`���𒅉�
	/// </summary>
	/// <param name="waitTime">�ҋ@����</param>
	private async Task CallWaitFollowUpCoroutine(int waitTime)
	{
		var wiatFollowUpTcs = new TaskCompletionSource<bool>();
		StartCoroutine(this._avatarMg.WaitFollowUp(wiatFollowUpTcs, waitTime));
		await wiatFollowUpTcs.Task;
	}
}