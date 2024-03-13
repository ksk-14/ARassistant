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

	// AudioSource�����̐ݒ�l�ɖ߂��ۂɎg�p
	private AudioClip _oriWorldAudioClip;
	private bool _oriWorldAudioIsLoop;
	private float _oriWorldAudioVolume;

	// �܂΂����Ɋւ��郁���o
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
		// �E�C���N�֌W
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
				// �܂΂��������Ȃ����͑��̕\��Ǘ������Ă��鎞�Ȃ̂Ŗڂ��J�����Ă���
				this._faceSkinnedMeshRenderer.SetBlendShapeWeight(this._eyeCloseIndex, 0.0f);
			}
			time += Time.deltaTime;
			yield return null;
		}
		// �Ō�͊m���ɖړIWeight�ɐݒ肷��
		this._faceSkinnedMeshRenderer.SetBlendShapeWeight(this._eyeCloseIndex, targetWeight);
			
	}

	/// <summary>�A�o�^�[�̍ŏ��̕Ԏ��ƃA�j���[�V�����̐ݒ�</summary>
	public void AvatarReplySetting() 
	{
		// �ŏ��̕Ԏ��������_���ɃZ�b�g
		this._avatarAudioSrc.clip = this._firstReplyClips[Random.Range(0, this._firstReplyClips.Length)];
		// �A�o�^�[���������͒��̃A�j���[�V�����ɕύX
		this.SetTrigger("VoiceListen");
	}

	/// <summary>�A�o�^�[���v���C���[�̕����Ɍ�����R���[�`��</summary>
	/// <param name="rotationDuration">�v���C���[�̕��������܂ł̎���</param>
	public IEnumerator AvatarLookPlayer(float duration)
	{
		// �A�o�^�[���猩���v���C���[�̃x�N�g�����Z�o
		Vector3 directionToPlayer = this._playerTf.position - this._avatarTf.position;
		// �x�N�g����Y���𖳎����邱�ƂŁA�A�o�^�[��X����]���Ȃ��悤�ɂ���
		directionToPlayer.y = 0;

		//�A�o�^�[�̌��݂̉�]��ۑ����A�v���C���[���������߂̖ڕW�̉�]���v�Z
		Quaternion startRotation = this._avatarTf.rotation;
		Quaternion endRotation = Quaternion.LookRotation(directionToPlayer);

		float elapsedTime = 0;

		// �o�ߎ��Ԃɉ����āA���X�ɃA�o�^�[���v���C���[�̕��֌�������
		while (elapsedTime < duration)
		{
			this._avatarTf.rotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / duration);
			//this._avatarTf.position = Vector3.Lerp(this._avatarTf.position, this._avatarInitPos, elapsedTime / duration);
			elapsedTime += Time.deltaTime;
			//this._avatarTf.position = this._avatarInitPos;
			yield return null;
		}

		// �m���ɖڕW�̌����ɐݒ�
		this._avatarTf.rotation = endRotation;
	}

	/// <summary> �A�o�^�[�̍ŏ��̉����ƁA�������͊J�nSE���Đ�����</summary>
	public IEnumerator PlayFirstReply()
	{
		// �ŏ��̕Ԏ����Đ����I����܂őҋ@
		this._avatarAudioSrc.Play();
		yield return new WaitWhile(() => this._avatarAudioSrc.isPlaying);
		yield return new WaitForSeconds(0.5f);
		yield return StartCoroutine(PlayInputStartSE());
	}

	/// <summary> �������͊J�nSE���Đ�����i�t�H���[�A�b�v�ɂ��Ή��j</summary>
	public IEnumerator PlayInputStartSE(TaskCompletionSource<bool> tcs=null)
	{
		this._worldAudioSrc.Play();
		yield return new WaitWhile(() => this._worldAudioSrc.isPlaying);
		if (tcs != null) tcs.SetResult(true);
	}

	/// <summary>�t�H���[�A�b�v�J�n�܂ł̑ҋ@����</summary>
	public IEnumerator WaitFollowUp(TaskCompletionSource<bool> tcs, int waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		tcs.SetResult(true);
	}

	/// <summary>�N���E�h�������i�V���L���O�^�C���j�̉��o���΂���</summary>
	public void StartThinking()
	{
		// �A�o�^�[���l�����̃A�j���[�V�����ɕύX
		this.SetTrigger("Thinking");
		this.SetWorldAudioSource(true, this._thikingClip, true, 0.2f);
		this._worldAudioSrc.Play();
	}

	/// <summary>�N���E�h�������i�V���L���O�^�C���j�̉��o�I������</summary>
	public void StopThinking()
	{
		// �A�o�^�[���l�����̃A�j���[�V��������߂�
		this.SetTrigger("ExitThinking");
		StartCoroutine(this.AvatarLookPlayer(0.2f));
		this._worldAudioSrc.Stop();
		this.SetWorldAudioSource(false);
	}

	/// <summary>S3����_�E�����[�h���������t�@�C����ǂݍ��݁A�Đ�����</summary>
	/// <param name="wavFilePath">���[�J���ɕۑ����ꂽ�����t�@�C���p�X</param>
	public IEnumerator LoadAndPlayReply(string wavFilePath, TaskCompletionSource<bool> tcs)
	{
		// UnityWebRequest���g�p���ĉ����t�@�C����ǂݍ��ށiAudioType.WAV or AudioType.MPEG�j
		using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + wavFilePath, AudioType.MPEG))
		{
			yield return www.SendWebRequest();

			if (www.result == UnityWebRequest.Result.Success)
			{
				// �����t�@�C���̓ǂݍ��݂ɐ��������ꍇ�AAudioClip���쐬���Đ�
				AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
				this._avatarAudioSrc.clip = clip;
				this._avatarAudioSrc.Play();
				yield return StartCoroutine(this.WaitPlayReplyComplete());
				tcs.SetResult(true);
			}
			else
			{
				Debug.LogError("�t�@�C���̓ǂݍ��݂Ɏ��s���܂���: " + www.error);
			}
		}
	}

	// �����Đ��I����ҋ@����R���[�`��
	private IEnumerator WaitPlayReplyComplete()
	{
		while (this._avatarAudioSrc.isPlaying)
		{
			yield return null;
		}
	}

	/// <summary>�n�b�L���O���o�𒅉΂���iIoT�@�푀�쎞�̂݌Ăԁj</summary>
	public IEnumerator StartHacking(TaskCompletionSource<bool> tcs, bool followUpFlg)
	{
		// �A�o�^�[���n�b�L���O�̃A�j���[�V�����ɕύX
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

	/// <summary>�A�o�^�[�̃A�j���[�V�����g���K�[���Z�b�g���鋤�ʃ��\�b�h</summary>
	/// <param name="triggerName">�g���K�[�p�����[�^�[��</param>
	public void SetTrigger(string triggerName)
	{
		this._avatarAnim.SetTrigger(triggerName);
	}

	/// <summary>
	/// ���[���hAudioSource�̏�Ԑݒ胁�\�b�h
	/// �ύX�O�̐ݒ�l��ۑ����A�C�ӂ̃^�C�~���O�Ō��ɖ߂����Ƃ��ł���
	/// </summary>
	/// <param name="isSet">TRUE=�V�����ݒ肷��, FALSE=���̏�Ԃɖ߂�</param>
	/// <param>���̈����́A�ݒ�����ɖ߂����ɂ͕s�v�Ȃ̂Ŕ�K�{�d�l</param>
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
