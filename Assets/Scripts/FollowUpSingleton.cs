using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using System;
using UnityEngine;

public class FollowUpSingleton : MonoBehaviour
{
	private static FollowUpSingleton _instance;
	private static readonly object  _lock = new object();
	public static FollowUpSingleton Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lock)
				{
					if (_instance == null)
					{
						GameObject obj = new GameObject("FollowUpSingleton");
						_instance = obj.AddComponent<FollowUpSingleton>();
						DontDestroyOnLoad(obj);
						return _instance;
					}
				}
			}
			return _instance;
		}
	}

	public bool followUpFlg { get; set; }
	public string questionPath { get; set; }
	public string yesPath { get; set; }
	public string noPath { get; set; }
	public string afterPath { get; set; }

}