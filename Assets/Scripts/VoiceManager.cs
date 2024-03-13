using UnityEngine;
using Oculus.Voice;
using System;

public class VoiceManager : MonoBehaviour
{
	[Header("Voice")]
	[SerializeField] private AppVoiceExperience _appVoiceExperience;

	private bool _isAppVoiceActive = false;

	// 各イベント時のコールバック関数を設定
	void Awake()
	{
		// 入力途中の音声をサーバーが文字起こしできた際の処理
		//this._appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener((transcription) => {
			// ここで途中入力の表示ができる
		//});

		// 入力完了した音声をサーバーが最終文字起こしできた際の処理
		this._appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener((transcription) => {
			Debug.Log($"音声入力内容：{transcription}");
		});
		
		// サーバーの全ての処理が終了した際の処理
		this._appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(() => {
			this._isAppVoiceActive = false;
		});
	}

	/// <summary>音声入力ONにする</summary>
	public void VoiceInputStart() 
	{
		if (!this._isAppVoiceActive) 
		{
			this._appVoiceExperience.Activate();
			this._isAppVoiceActive = true;
		}
	}

	/// <summary>WitAIの解析データを元にJSONファイル作成</summary>
	/// <param name="analysisData">WitAIの解析データ</param>
	/// <param name="intent">WitAIの解析結果の検出インテント</param>
	/// <returns>JSONデータとJSONファイル名（タプル型）</returns>
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

		// ユニークなファイル名を作成
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