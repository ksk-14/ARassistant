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
		// S3�N���C�A���g�擾
		S3ClientSingleton s3ClientSingleton = S3ClientSingleton.Instance;
		this._s3Client = s3ClientSingleton.GetS3Client();
		this._followUpIns = FollowUpSingleton.Instance;

		// ���ݒ�t�@�C������K�v�ȃf�[�^���擾
		this._backetName = ConfigSingleton.Instance.Config.S3BucketName;
		this._prefixReply = ConfigSingleton.Instance.Config.S3PrefixReply;
		this._prefixAskAgain = ConfigSingleton.Instance.Config.S3PrefixAskAgain;
	}

	/// <summary>�񓯊���S3���特���t�@�C�����_�E�����[�h����</summary>
	/// <returns>���[�J���ɕۑ����ꂽ�����t�@�C���p�X</returns>
	public async Task<string> DownloadWavAsync(bool followUpFlg = false)
	{
		// �ŐV�̃t�@�C�������擾
		List<string> latestFilePaths = await GetLatestFileNameAsync(followUpFlg);

		if (latestFilePaths == null)
		{
			//TODO:�G���[�n���h�����O
			Debug.LogError("S3�̍ŐV�t�@�C����������Ȃ�����");
		}
		// �t�@�C�����_�E�����[�h
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

		// �ŏI�X�V�����Ń\�[�g���A�ŐV�̃t�@�C������C�ӂ̐��擾
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

				// S3����̃��X�|���X���擾�i�_�E�����[�h�͂܂��j
				using (GetObjectResponse response = await this._s3Client.GetObjectAsync(request))
				// ���X�|���X����̃f�[�^�X�g���[���ɃA�N�Z�X
				using (Stream responseStream = response.ResponseStream)
				// �X�g���[���t�@�C�������[�J���ɍ쐬
				using (FileStream fileStream = File.Create(filePath)){
					// �f�[�^�X�g���[�������[�J���t�@�C���ɔ񓯊��ŃR�s�[�i�_�E�����[�h�J�n�j
					await responseStream.CopyToAsync(fileStream);
				}
				localFilePaths.Add(filePath);
			}
			catch (AmazonS3Exception e)
			{
				//TODO:�G���[�n���h�����O
				Debug.LogError($"S3�_�E�����[�h�G���[����:{e.Message}");
			}
			catch (System.Exception e)
			{
				//TODO:�G���[�n���h�����O
				Debug.LogError($"�_�E�����[�h�G���[������:{e.Message}");
			}
		}
		return localFilePaths;
	}
}