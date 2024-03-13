using UnityEngine;
using Oculus.Voice;
using System;

public class VoiceManager : MonoBehaviour
{
	[Header("Voice")]
	[SerializeField] private AppVoiceExperience _appVoiceExperience;

	private bool _isAppVoiceActive = false;

	// �e�C�x���g���̃R�[���o�b�N�֐���ݒ�
	void Awake()
	{
		// ���͓r���̉������T�[�o�[�������N�����ł����ۂ̏���
		//this._appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener((transcription) => {
			// �����œr�����͂̕\�����ł���
		//});

		// ���͊��������������T�[�o�[���ŏI�����N�����ł����ۂ̏���
		this._appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener((transcription) => {
			Debug.Log($"�������͓��e�F{transcription}");
		});
		
		// �T�[�o�[�̑S�Ă̏������I�������ۂ̏���
		this._appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(() => {
			this._isAppVoiceActive = false;
		});
	}

	/// <summary>��������ON�ɂ���</summary>
	public void VoiceInputStart() 
	{
		if (!this._isAppVoiceActive) 
		{
			this._appVoiceExperience.Activate();
			this._isAppVoiceActive = true;
		}
	}

	/// <summary>WitAI�̉�̓f�[�^������JSON�t�@�C���쐬</summary>
	/// <param name="analysisData">WitAI�̉�̓f�[�^</param>
	/// <param name="intent">WitAI�̉�͌��ʂ̌��o�C���e���g</param>
	/// <returns>JSON�f�[�^��JSON�t�@�C�����i�^�v���^�j</returns>
	public (string, string) MakeVoiceJson(string[] analysisData, string intent)
	{
		string json = string.Empty;
		if (intent == "SwitchBot")
		{
			json = this.MakeSwitchBotJson(analysisData);
		}
		else if (intent == "Info")
		{
			json = this.MakeInfoJson(analysisData);
		}

		// ���j�[�N�ȃt�@�C�������쐬
		string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
		string jsonFileName = $"EdgeData/{timestamp}.json";

		return (json, jsonFileName);
	}

	private string MakeSwitchBotJson(string[] analysisData)
	{
		CreateSwitchBotJson voiceJson = new CreateSwitchBotJson();
		voiceJson.appliance = analysisData[0];
		voiceJson.action = analysisData[1];
		return JsonUtility.ToJson(voiceJson);
	}

	private string MakeInfoJson(string[] analysisData)
	{
		CreateInfoJson voiceJson = new CreateInfoJson();
		voiceJson.kind = analysisData[0];
		voiceJson.request = analysisData[1];
		return JsonUtility.ToJson(voiceJson);
	}
}

[System.Serializable]
public class CreateSwitchBotJson
{
	public string appliance;
	public string action;
}

[System.Serializable]
public class CreateInfoJson
{
	public string kind;
	public string request;
}