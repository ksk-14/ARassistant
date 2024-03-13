using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using UnityEngine;

public class S3DownloadManager : MonoBehaviour
{
	private string _backetName;
	private string _prefixReply;
	private string _prefixAskAgain;
	
	private AmazonS3Client _s3Client;
	private FollowUpSingleton _followUpIns;

	private void Start()
	{
		// S3クライアント取得
		S3ClientSingleton s3ClientSingleton = S3ClientSingleton.Instance;
		this._s3Client = s3ClientSingleton.GetS3Client();
		this._followUpIns = FollowUpSingleton.Instance;

		// 環境設定ファイルから必要なデータを取得
		this._backetName = ConfigSingleton.Instance.Config.S3BucketName;
		this._prefixReply = ConfigSingleton.Instance.Config.S3PrefixReply;
		this._prefixAskAgain = ConfigSingleton.Instance.Config.S3PrefixAskAgain;
	}

	/// <summary>非同期でS3から音声ファイルをダウンロードする</summary>
	/// <returns>ローカルに保存された音声ファイルパス</returns>
	public async Task<string> DownloadWavAsync(bool followUpFlg = false)
	{
		// 最新のファイル名を取得
		List<string> latestFilePaths = await GetLatestFileNameAsync(followUpFlg);

		if (latestFilePaths == null)
		{
			//TODO:エラーハンドリング
			Debug.LogError("S3の最新ファイルが見つからなかった");
		}
		// ファイルをダウンロード
		List<string> wavFilesPath = await DownloadFileAsync(latestFilePaths);
		return followUpFlg ? await this.SaveFollowUpState(wavFilesPath) : wavFilesPath[0];
	}

	private async Task<String> SaveFollowUpState(List<string> wavFilesPath)
	{
		this._followUpIns.questionPath = wavFilesPath[0];
		this._followUpIns.yesPath = wavFilesPath[1];
		this._followUpIns.noPath = wavFilesPath[2];
		this._followUpIns.afterPath = wavFilesPath[3];
		return null;
	}

	private async Task<List<string>> GetLatestFileNameAsync(bool askAgainFlg)
	{
		string prefix = askAgainFlg ? this._prefixAskAgain : this._prefixReply;
		int fileNum = askAgainFlg ? 4 : 1;

		var request = new ListObjectsV2Request
		{
			BucketName = this._backetName,
			Prefix = prefix + "/"
		};

		var response = await this._s3Client.ListObjectsV2Async(request);
		var files = response.S3Objects;

		// 最終更新日時でソートし、最新のファイル名を任意の数取得
		List<string>  latestFiles = files.OrderByDescending(f => f.LastModified)
													.Take(fileNum)
													.Select(f => f.Key)
													.ToList();
		return latestFiles;
	}

	private async Task<List<string>> DownloadFileAsync(List<string> latestFilePaths)
	{
		List<string> localFilePaths = new List<string>();

		foreach (string latestFilePath in latestFilePaths)
		{
			string filePath = Path.Combine(Application.persistentDataPath, Path.GetFileName(latestFilePath));

			try
			{
				GetObjectRequest request = new GetObjectRequest
				{
					BucketName = this._backetName,
					Key =  latestFilePath
				};

				// S3からのレスポンスを取得（ダウンロードはまだ）
				using (GetObjectResponse response = await this._s3Client.GetObjectAsync(request))
				// レスポンスからのデータストリームにアクセス
				using (Stream responseStream = response.ResponseStream)
				// ストリームファイルをローカルに作成
				using (FileStream fileStream = File.Create(filePath)){
					// データストリームをローカルファイルに非同期でコピー（ダウンロード開始）
					await responseStream.CopyToAsync(fileStream);
				}
				localFilePaths.Add(filePath);
			}
			catch (AmazonS3Exception e)
			{
				//TODO:エラーハンドリング
				Debug.LogError($"S3ダウンロードエラー発生:{e.Message}");
			}
			catch (System.Exception e)
			{
				//TODO:エラーハンドリング
				Debug.LogError($"ダウンロードエラーが発生:{e.Message}");
			}
		}
		return localFilePaths;
	}
}