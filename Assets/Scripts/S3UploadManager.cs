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
	/// 非同期のS3アップロード処理
	/// </summary>
	/// <param name="jsonData">JSONデータとJSONファイル名（タプル型）</param>
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
			Debug.Log("S3アップロード成功");
		}
		catch (Exception e)
		{
			//TODO:エラーハンドリング
			Debug.LogError($"S3アップロード失敗: {e}");
		}
	}
}