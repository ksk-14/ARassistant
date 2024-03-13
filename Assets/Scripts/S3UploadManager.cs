using UnityEngine;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Threading.Tasks;

public class S3UploadManager : MonoBehaviour
{
	[SerializeField] private string _accessKeyId;
	[SerializeField] private string _secretAccessKey;
	[SerializeField] private string _regionEndpoint;
	[SerializeField] private string _bucketName;

	private AmazonS3Client _s3Client;

	private void Start()
	{
		S3ClientSingleton s3ClientSingleton = S3ClientSingleton.Instance;
		this._s3Client = s3ClientSingleton.GetS3Client();
	}

	/// <summary>
	/// �񓯊���S3�A�b�v���[�h����
	/// </summary>
	/// <param name="jsonData">JSON�f�[�^��JSON�t�@�C�����i�^�v���^�j</param>
	public async Task S3UploadAsync((string, string) jsonData)
	{
		PutObjectRequest request = new PutObjectRequest
		{
			BucketName = this._bucketName,
			Key = jsonData.Item2,
			ContentBody = jsonData.Item1
		};

		try
		{
			PutObjectResponse response = await this._s3Client.PutObjectAsync(request);
			Debug.Log("S3�A�b�v���[�h����");
		}
		catch (Exception e)
		{
			//TODO:�G���[�n���h�����O
			Debug.LogError($"S3�A�b�v���[�h���s: {e}");
		}
	}
}