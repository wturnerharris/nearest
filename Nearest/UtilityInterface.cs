
using System.IO;

namespace Nearest
{
	public interface IUtility
	{
		bool FileExists(string filename);

		void CopyFromAssets(string toPath);

		void CopyTo(Stream stream, string toPath);

		void UnzipFile(string zipFilePath);

		void DeleteFile(string path);

		string CombinePath(string path1, string path2);

		void WriteLine(string msg);
	}
}

