using System;
using System.IO;
using System.IO.Compression;
using Android.App;
using Android.Util;

namespace Nearest.Droid
{
	public class Utility : IUtility
	{
		public string CopyDatabaseFromAssets(string dbFileName)
		{
			string dbFilePath = Application.Context.GetDatabasePath(dbFileName).AbsolutePath;
			string databaseFolder = Path.GetDirectoryName(dbFilePath);

			if (!File.Exists(dbFilePath))
			{
				if (!Directory.Exists(databaseFolder))
				{
					Directory.CreateDirectory(databaseFolder);
				}
				Stream fileStream = Application.Context.Assets.Open(dbFileName);
				using (BinaryReader br = new BinaryReader(fileStream))
				{
					using (BinaryWriter bw = new BinaryWriter(new FileStream(dbFilePath, FileMode.Create)))
					{
						byte[] buffer = new byte[2048];
						int len = 0;
						while ((len = br.Read(buffer, 0, buffer.Length)) > 0)
						{
							bw.Write(buffer, 0, len);
						}
					}
				}
			}
			return dbFilePath;
		}

		public void CopyFromAssets(string toPath)
		{
			var FileName = Path.GetFileName(toPath);
			var PathDir = Path.GetDirectoryName(toPath);

			//Should not be a directory
			if (Directory.Exists(toPath))
			{
				Directory.Delete(toPath);
			}

			//Create file from target asset
			if (!File.Exists(toPath))
			{
				if (!Directory.Exists(PathDir))
				{
					Directory.CreateDirectory(PathDir);
				}
				Stream fileStream = Application.Context.Assets.Open(FileName);
				using (BinaryReader br = new BinaryReader(fileStream))
				{
					using (BinaryWriter bw = new BinaryWriter(new FileStream(toPath, FileMode.Create)))
					{
						byte[] buffer = new byte[2048];
						int len = 0;
						while ((len = br.Read(buffer, 0, buffer.Length)) > 0)
						{
							bw.Write(buffer, 0, len);
						}
					}
				}
			}
		}

		public void CopyTo(Stream fileStream, string toPath)
		{
			var PathDir = Path.GetDirectoryName(toPath);

			//Should not be a directory
			if (Directory.Exists(toPath))
			{
				Directory.Delete(toPath);
			}

			//Create file from target asset
			if (!File.Exists(toPath))
			{
				if (!Directory.Exists(PathDir))
				{
					Directory.CreateDirectory(PathDir);
				}
				using (StreamReader Reader = new StreamReader(fileStream))
				{
					using (StreamWriter Writer = new StreamWriter(toPath))
					{
						string Content = Reader.ReadToEnd();
						Writer.Write(Content);
						Reader.Close();
						Writer.Close();
					}
				}
			}
		}

		public void UnzipFile(string zipFilePath)
		{
			var zipFile = new FileInfo(zipFilePath);

			try
			{
				using (FileStream zipStream = zipFile.OpenRead())
				{
					string fileName = zipFile.FullName;
					int extIndex = fileName.Length - zipFile.Extension.Length;
					string newFileName = fileName.Remove(extIndex);

					using (FileStream extracted = File.Create(newFileName))
					{
						using (GZipStream extraction = new GZipStream(zipStream, CompressionMode.Decompress))
						{
							extraction.CopyTo(extracted);
							Console.WriteLine("Decompressed: {0}", zipFile.Name);
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Debug("Decompress", "unzip {0}", e.Message);
			}

		}

		public void DeleteFile(string path)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		public string CombinePath(string path1, string path2)
		{
			return Path.Combine(path1, path2);
		}

		public bool FileExists(string filename)
		{
			if (!Directory.Exists(Path.GetDirectoryName(filename)))
			{
				return false;
			}
			if (!File.Exists(filename))
			{
				return false;
			}
			return true;
		}

		public string GetDataPath()
		{
			var appDataPath = Application.Context.GetExternalFilesDir(null).Path;
			return appDataPath;
		}

		public void WriteLine(string msg)
		{
			Log.Debug("Nearest", "DBG: " + msg);
		}
	}
}

