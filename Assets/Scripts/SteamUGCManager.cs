using System;
using System.Collections;
using UnityEngine;
using Steamworks;
using System.IO;
using System.Threading.Tasks;
using Steamworks.Data;
using Steamworks.Ugc;

namespace GameOverlay
{
	public class SteamUGCManager
	{
		public const ulong PUBLISH_ITEM_FAILED_CODE = 0u;
		public const uint APP_ID = 635260;
		private const string MAP_TAG = "3";
		private bool m_isUploading;
		
		public void Update()
		{
			SteamClient.RunCallbacks();
		}

		private IEnumerator UpdateItemCoroutine(string path, ulong id)
		{
			var dirInfo = new DirectoryInfo(path);
			
			var itemTask = Item.GetAsync(id);
			yield return itemTask.AsIEnumerator();

			var itemTaskResult = itemTask.Result;
			if (!itemTaskResult.HasValue)
			{
				Debug.LogError($"SteamUGCManager : Unable to get item with id {id}");
				yield break;
			}

			var item = itemTaskResult.Value;
			var editor = EditItemContent(item, dirInfo);
			var submitTask = editor.SubmitAsync();
			
			yield return submitTask.AsIEnumerator();

			var submitTaskResult = submitTask.Result;

			Debug.Log($"UGC item ({submitTaskResult.FileId}) update finished with result '{submitTaskResult.Result}'");
		}

		private FileInfo TryGetMetaFileFromDir(DirectoryInfo dirInfo)
		{
			var previewFiles = dirInfo.GetFiles("meta");
			foreach (var file in previewFiles)
			{
				if (file.Name.ToLower().Contains("meta"))
				{
					return file;
				}
			}

			return null;
		}
		
		protected virtual Editor EditItemContent(Item item, DirectoryInfo dirInfo)
		{
			var editor = item.Edit().WithContent(dirInfo);

			var metaFileInfo = TryGetMetaFileFromDir(dirInfo);
			if (metaFileInfo != null)
			{
				editor.WithMetaData(metaFileInfo.DirectoryName + "/" + metaFileInfo.Name);
				editor.WithTitle(m_itemName);
				editor.WithPreviewFile(m_previewPath);
				editor.WithTag(MAP_TAG);
				editor.WithDescription(m_desciption);
				editor.WithPrivateVisibility();
			}

			return editor;
		}

		private Editor CreateCommunityFile()
		{     
			return Editor.NewCommunityFile
				.ForAppId(APP_ID)
				.WithTag(MAP_TAG)
				.WithPrivateVisibility();
		}

		private Task<PublishResult> m_currentPublishResult;
		private string m_itemName;
		private string m_previewPath;
		private string m_desciption;
		
		public void SetItemData(string itemName, string previewPath, string desciption)
		{
			m_itemName = itemName;
			m_previewPath = previewPath;
			m_desciption = desciption;
		}
		
		public IEnumerator CreatePublisherItem(Action<PublishResult> onCreate)
		{
			var submitTask = CreateCommunityFile().SubmitAsync();
			m_currentPublishResult = submitTask;
			yield return m_currentPublishResult.AsIEnumerator();
			onCreate?.Invoke(m_currentPublishResult.Result);
		}
		
		public IEnumerator PublishItemCoroutine(string path, Action<ulong> uploadedId)
		{
			m_isUploading = true;
			
			yield return m_currentPublishResult.AsIEnumerator();

			var submitTaskResult = m_currentPublishResult.Result;

			Debug.Log($"UGC item ({submitTaskResult.FileId}) creation finished with result '{submitTaskResult.Result}'");

			if (submitTaskResult.Result == Result.OK)
			{
				ulong itemId = submitTaskResult.FileId.Value;

				File.WriteAllText(path + Path.AltDirectorySeparatorChar + "pid.txt", itemId.ToString());

				yield return UpdateItemCoroutine(path, itemId);
				uploadedId.Invoke(itemId);
			}
			else
			{
				uploadedId.Invoke(PUBLISH_ITEM_FAILED_CODE);
			}

			m_isUploading = false;
		}

		public IEnumerator UploadItemCoroutine(string path, PublishedFileId itemId, Action<ulong> uploadedId = null)
		{
			yield return UpdateItemCoroutine(path, itemId);
			uploadedId?.Invoke(itemId);
		}
	}
}

public static class ExtensionMethods 
{
	public static IEnumerator AsIEnumerator(this Task task)
	{
		while (!task.IsCompleted)
		{
			yield return null;
		}

		if (task.IsFaulted && task.Exception != null)
		{
			throw task.Exception;
		}
	}
}