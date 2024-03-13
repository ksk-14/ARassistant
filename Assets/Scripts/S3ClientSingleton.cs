using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using System;
using UnityEngine;
using static OVRHaptics;

public class S3ClientSingleton : MonoBehaviour
{
	private static string _identityPoolId;
	private static string _regionEndpoint;

	private static S3ClientSingleton _instance;
	private static readonly object _lock = new object();
	private AmazonS3Client _s3Client;

	public static S3ClientSingleton Instance
	{
		get
		{
			lock (_lock)
			{
				if (_instance == null)
				{
					GameObject obj = new GameObject("S3ClientSingleton");
					_instance = obj.AddComponent<S3ClientSingleton>();
					DontDestroyOnLoad(obj);
					return _instance;
				}
			}
			return _instance;
		}
	}

	public AmazonS3Client GetS3Client()
	{
		if (this._s3Client == null)
		{
			this.LoadConfig();
			this.InitializeS3Client();
		}
		return this._s3Client;
	}

	private void LoadConfig()
	{
		_identityPoolId = ConfigSingleton.Instance.Config.IdentityPoolId;
		_regionEndpoint = ConfigSingleton.Instance.Config.RegionEndpoint;
	}

	private void InitializeS3Client()
	{
		try
		{
			RegionEndpoint region = RegionEndpoint.GetBySystemName(_regionEndpoint);
			CognitoAWSCredentials credentials = new CognitoAWSCredentials(_identityPoolId, region);
			this._s3Client = new AmazonS3Client(credentials, region);
		}
		catch (Exception e)
		{
			//TODO:エラーハンドリング
			Debug.LogError($"S3クライアント生成失敗: {e}");
		}
	}
}
