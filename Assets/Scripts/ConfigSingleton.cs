using System;
using UnityEngine;

public class ConfigSingleton : MonoBehaviour
{
	public static ConfigSingleton Instance { get; private set; }
	public ConfigData Config { get; private set; }

	[Serializable]
	public class ConfigData
	{
		public string IdentityPoolId;
		public string RegionEndpoint;
		public string ServerURL;
		public string ServerPathPoll;
		public string ServerPathSendPlayed;
		public string ServerPathSendAnswer;
		public string S3BucketName;
		public string S3PrefixReply;
		public string S3PrefixAskAgain;
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(this.gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(this.gameObject);

		LoadConfig();
	}

	private void LoadConfig()
	{
		TextAsset configText = Resources.Load<TextAsset>("config");
		if (configText != null)
		{
			Config = JsonUtility.FromJson<ConfigData>(configText.text);
		}
		else
		{
			//TODO:エラーハンドリング
			Debug.LogError("Configファイルが見つからなかった");
		}
	}
}